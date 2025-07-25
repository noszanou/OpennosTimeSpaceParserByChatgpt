using NosTaleTimeSpaceParser.Models.PacketModels;
using NosTaleTimeSpaceParser.Models.XmlModels;
using NosTaleTimeSpaceParser.Parsers;
using ScriptedInstanceModel.Models.ScriptedInstance;

namespace NosTaleTimeSpaceParser.Services
{
    public class TimeSpaceAnalyzer
    {
        private List<string> _packets;
        private ScriptedInstanceModel _model;
        private Dictionary<int, CreateMap> _maps;
        private Dictionary<int, List<string>> _mapPackets;
        private string? _descriptionLine;

        public TimeSpaceAnalyzer()
        {
            _packets = new List<string>();
            _model = new ScriptedInstanceModel();
            _maps = new Dictionary<int, CreateMap>();
            _mapPackets = new Dictionary<int, List<string>>();
        }

        public ScriptedInstanceModel Analyze(List<string> packets)
        {
            _packets = packets;
            _model = new ScriptedInstanceModel();
            _maps = new Dictionary<int, CreateMap>();
            _mapPackets = new Dictionary<int, List<string>>();

            ExtractDescriptionLine();
            ParseGlobalsFromRbr();
            IdentifyMapsFromAtPackets();
            AssignPacketsToMaps();
            GenerateMapEvents();
            FinalizeModel();

            return _model;
        }

        private void ExtractDescriptionLine()
        {
            for (int i = 0; i < _packets.Count; i++)
            {
                if (RbrPacketParser.CanParse(_packets[i]) && i + 1 < _packets.Count)
                {
                    var nextLine = _packets[i + 1].Trim();
                    if (!nextLine.StartsWith("su ") && !string.IsNullOrEmpty(nextLine) && !IsPacketLine(nextLine))
                    {
                        _descriptionLine = nextLine;
                    }
                    break;
                }
            }
        }

        private bool IsPacketLine(string line)
        {
            var packetPrefixes = new[] { "at ", "in ", "gp ", "msg ", "walk ", "su ", "eff ", "evnt ", "npc_req ", "out ", "preq", "rsfn ", "rsfm ", "rsfp " };
            return packetPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private void ParseGlobalsFromRbr()
        {
            foreach (var packet in _packets)
            {
                if (RbrPacketParser.CanParse(packet))
                {
                    var rbr = RbrPacketParser.Parse(packet);

                    _model.Globals.Name.Value = rbr.Name;
                    _model.Globals.Label.Value = !string.IsNullOrEmpty(_descriptionLine) ? _descriptionLine : rbr.Label;
                    _model.Globals.LevelMinimum.Value = rbr.LevelMinimum.ToString();
                    _model.Globals.LevelMaximum.Value = rbr.LevelMaximum.ToString();

                    // Valeurs par défaut pour les champs manquants dans RbrPacket
                    _model.Globals.Lives.Value = "1";
                    _model.Globals.Gold.Value = "1500";
                    _model.Globals.Reputation.Value = "50";

                    foreach (var item in rbr.DrawItems)
                    {
                        _model.Globals.DrawItems.Items.Add(new ItemElement { VNum = item.VNum, Amount = item.Amount });
                    }

                    foreach (var item in rbr.SpecialItems)
                    {
                        _model.Globals.SpecialItems.Items.Add(new ItemElement { VNum = item.VNum, Amount = item.Amount });
                    }

                    foreach (var item in rbr.GiftItems)
                    {
                        _model.Globals.GiftItems.Items.Add(new ItemElement { VNum = item.VNum, Amount = item.Amount });
                    }

                    break;
                }
            }
        }

        private void IdentifyMapsFromAtPackets()
        {
            int mapIndex = 0;

            for (int i = 0; i < _packets.Count; i++)
            {
                if (AtPacketParser.CanParse(_packets[i]))
                {
                    var at = AtPacketParser.Parse(_packets[i]);

                    var createMap = new CreateMap
                    {
                        Map = mapIndex,
                        VNum = at.GridMapId,
                        IndexX = 3,
                        IndexY = 11 - mapIndex
                    };

                    _maps[mapIndex] = createMap;
                    _mapPackets[mapIndex] = new List<string>();

                    mapIndex++;
                }
            }
        }

        private void AssignPacketsToMaps()
        {
            int currentMapIndex = -1;

            for (int i = 0; i < _packets.Count; i++)
            {
                var packet = _packets[i];

                if (AtPacketParser.CanParse(packet))
                {
                    currentMapIndex++;
                    continue;
                }

                if (currentMapIndex >= 0 && _mapPackets.ContainsKey(currentMapIndex))
                {
                    _mapPackets[currentMapIndex].Add(packet);
                }
            }
        }

        private void GenerateMapEvents()
        {
            foreach (var mapKvp in _maps)
            {
                int mapIndex = mapKvp.Key;
                var map = mapKvp.Value;
                var packets = _mapPackets[mapIndex];

                AnalyzeMapPackets(map, packets, mapIndex);
            }
        }

        private void AnalyzeMapPackets(CreateMap map, List<string> packets, int mapIndex)
        {
            ProcessPortals(map, packets, mapIndex);

            var discoveringPackets = new List<string>();
            var moveOnMapPackets = new List<string>();
            bool inMovePhase = false;

            for (int i = 0; i < packets.Count; i++)
            {
                var packet = packets[i];

                if (WalkPacketParser.CanParse(packet) && !inMovePhase)
                {
                    inMovePhase = true;
                    continue;
                }

                if (inMovePhase)
                    moveOnMapPackets.Add(packet);
                else
                    discoveringPackets.Add(packet);
            }

            if (discoveringPackets.Any())
            {
                map.OnCharacterDiscoveringMap = new OnCharacterDiscoveringMap();
                ProcessDiscoveringPhase(map.OnCharacterDiscoveringMap, discoveringPackets, packets, mapIndex);
            }

            if (moveOnMapPackets.Any())
            {
                map.OnMoveOnMap = new OnMoveOnMap();
                ProcessMovePhase(map.OnMoveOnMap, moveOnMapPackets, packets, mapIndex);
            }
        }

        private void ProcessPortals(CreateMap map, List<string> packets, int mapIndex)
        {
            var processedPortals = new HashSet<string>();

            foreach (var packet in packets)
            {
                if (GpPacketParser.CanParse(packet))
                {
                    var gp = GpPacketParser.Parse(packet);
                    var portalKey = $"{gp.PortalId}_{gp.SourceX}_{gp.SourceY}";

                    if (processedPortals.Contains(portalKey))
                        continue;

                    processedPortals.Add(portalKey);

                    int toMap = CalculateToMap(gp, mapIndex);
                    int toX = gp.SourceX;
                    int toY = gp.SourceY == 1 ? 28 : 1;

                    var portal = new SpawnPortal
                    {
                        IdOnMap = gp.PortalId,
                        PositionX = gp.SourceX,
                        PositionY = gp.SourceY,
                        Type = gp.Type,
                        ToMap = toMap,
                        ToX = toX,
                        ToY = toY
                    };

                    if (gp.Type == 5)
                    {
                        portal.ToMap = -1;
                        portal.OnTraversal = new OnTraversal();
                        portal.OnTraversal.Ends.Add(new EndElement { Type = 5 });
                    }

                    map.SpawnPortals.Add(portal);
                }
            }
        }

        private int CalculateToMap(GpPacket gp, int currentMapIndex)
        {
            if (gp.Type == 5) return -1;

            if (gp.SourceY == 1)
                return currentMapIndex + 1 < _maps.Count ? currentMapIndex + 1 : currentMapIndex;
            else if (gp.SourceY == 28)
                return currentMapIndex > 0 ? currentMapIndex - 1 : currentMapIndex;

            return currentMapIndex;
        }

        private void ProcessDiscoveringPhase(OnCharacterDiscoveringMap discovering, List<string> packets, List<string> allPackets, int mapIndex)
        {
            for (int i = 0; i < packets.Count; i++)
            {
                var packet = packets[i];

                if (MsgPacketParser.CanParse(packet))
                {
                    var msg = MsgPacketParser.Parse(packet);
                    discovering.SendMessages.Add(new SendMessage
                    {
                        Value = msg.Message,
                        Type = msg.Type
                    });
                }
                else if (NpcReqPacketParser.CanParse(packet))
                {
                    var npcReq = NpcReqPacketParser.Parse(packet);
                    discovering.NpcDialogs.Add(new ValueAttribute { Value = npcReq.DialogId.ToString() });
                }
                else if (packet.Trim().StartsWith("sinfo ") || packet.Trim().StartsWith("rsfm "))
                {
                    discovering.SendPackets.Add(new ValueAttribute { Value = packet.Trim() });
                }
                else if (InPacketParser.CanParse(packet))
                {
                    var inPacket = InPacketParser.Parse(packet);

                    if (inPacket.EntityType == EntityType.Npc)
                    {
                        var npc = new SummonNpc
                        {
                            VNum = inPacket.VNum,
                            PositionX = inPacket.PositionX,
                            PositionY = inPacket.PositionY,
                            Move = true,
                            IsProtected = DetermineIsProtected(inPacket, allPackets, packets, i)
                        };

                        var onDeath = DetectNpcOnDeath(inPacket, allPackets, packets, i);
                        if (onDeath != null)
                        {
                            npc.OnDeath = onDeath;
                        }

                        discovering.SummonNpcs.Add(npc);
                    }
                    else if (inPacket.EntityType == EntityType.Object)
                    {
                        var button = new SpawnButton
                        {
                            Id = inPacket.EntityId,
                            PositionX = inPacket.PositionX,
                            PositionY = inPacket.PositionY,
                            VNumEnabled = inPacket.VNum,
                            VNumDisabled = FindAlternateVNum(inPacket, allPackets, i)
                        };

                        button.OnFirstEnable = new OnFirstEnable();
                        discovering.SpawnButtons.Add(button);
                    }
                }
            }
        }

        private void ProcessMovePhase(OnMoveOnMap moveOnMap, List<string> packets, List<string> allPackets, int mapIndex)
        {
            for (int i = 0; i < packets.Count; i++)
            {
                var packet = packets[i];

                if (MsgPacketParser.CanParse(packet))
                {
                    var msg = MsgPacketParser.Parse(packet);
                    moveOnMap.SendMessages.Add(new SendMessage
                    {
                        Value = msg.Message,
                        Type = msg.Type
                    });
                }
                else if (packet.Trim().StartsWith("sinfo ") || packet.Trim().StartsWith("rsfm "))
                {
                    moveOnMap.SendPackets.Add(new ValueAttribute { Value = packet.Trim() });
                }
                else if (EvntPacketParser.CanParse(packet))
                {
                    var evnt = EvntPacketParser.Parse(packet);
                    moveOnMap.GenerateClocks.Add(new ValueAttribute { Value = evnt.Time1.ToString() });
                    moveOnMap.StartClocks.Add(new object());
                }
                else if (InPacketParser.CanParse(packet))
                {
                    var inPacket = InPacketParser.Parse(packet);

                    if (inPacket.EntityType == EntityType.Monster)
                    {
                        var monster = new SummonMonster
                        {
                            VNum = inPacket.VNum,
                            PositionX = inPacket.PositionX,
                            PositionY = inPacket.PositionY,
                            Move = true,
                            IsBonus = DetermineIsBonus(inPacket, allPackets, packets, i),
                            IsHostile = true,
                            IsTarget = false,
                            IsBoss = false,
                            IsMeteorite = false,
                            Damage = 0,
                            NoticeRange = 0,
                            HasDelay = 0
                        };

                        var onDeath = DetectMonsterOnDeath(inPacket, allPackets, packets, i);
                        if (onDeath != null)
                        {
                            monster.OnDeath = onDeath;
                        }

                        moveOnMap.SummonMonsters.Add(monster);
                    }
                    else if (inPacket.EntityType == EntityType.Npc)
                    {
                        var npc = new SummonNpc
                        {
                            VNum = inPacket.VNum,
                            PositionX = inPacket.PositionX,
                            PositionY = inPacket.PositionY,
                            Move = true,
                            IsProtected = DetermineIsProtected(inPacket, allPackets, packets, i)
                        };

                        var onDeath = DetectNpcOnDeath(inPacket, allPackets, packets, i);
                        if (onDeath != null)
                        {
                            npc.OnDeath = onDeath;
                        }

                        moveOnMap.SummonNpcs.Add(npc);
                    }
                }
            }
        }

        private bool DetermineIsProtected(InPacket npcPacket, List<string> allPackets, List<string> phasePackets, int index)
        {
            for (int i = Math.Max(0, index - 5); i < Math.Min(phasePackets.Count, index + 10); i++)
            {
                if (MsgPacketParser.CanParse(phasePackets[i]))
                {
                    var msg = MsgPacketParser.Parse(phasePackets[i]);
                    if (msg.Message.ToLower().Contains("protect") || msg.Message.ToLower().Contains("guard"))
                    {
                        return true;
                    }
                }
                else if (phasePackets[i].Contains("sinfo") &&
                        (phasePackets[i].ToLower().Contains("protect") || phasePackets[i].ToLower().Contains("guard")))
                {
                    return true;
                }
            }

            return false;
        }

        private OnDeath? DetectNpcOnDeath(InPacket npcPacket, List<string> allPackets, List<string> phasePackets, int index)
        {
            if (DetermineIsProtected(npcPacket, allPackets, phasePackets, index))
            {
                var onDeath = new OnDeath();
                onDeath.Ends.Add(new EndElement { Type = 2 });
                return onDeath;
            }

            return null;
        }

        private bool DetermineIsBonus(InPacket monsterPacket, List<string> allPackets, List<string> phasePackets, int index)
        {
            for (int i = Math.Max(0, index - 3); i < Math.Min(phasePackets.Count, index + 8); i++)
            {
                if (SuPacketParser.CanParse(phasePackets[i]))
                {
                    var su = SuPacketParser.Parse(phasePackets[i]);
                    if (Math.Abs(su.PositionX - monsterPacket.PositionX) <= 3 &&
                        Math.Abs(su.PositionY - monsterPacket.PositionY) <= 3)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private OnDeath? DetectMonsterOnDeath(InPacket monsterPacket, List<string> allPackets, List<string> phasePackets, int index)
        {
            var processedPortals = new HashSet<int>();
            var processedMessages = new HashSet<string>();
            var processedDialogs = new HashSet<int>();
            bool foundEvents = false;
            var onDeath = new OnDeath();

            for (int i = index + 1; i < Math.Min(phasePackets.Count, index + 25); i++)
            {
                if (SuPacketParser.CanParse(phasePackets[i]))
                {
                    var su = SuPacketParser.Parse(phasePackets[i]);
                    if (Math.Abs(su.PositionX - monsterPacket.PositionX) <= 3 &&
                        Math.Abs(su.PositionY - monsterPacket.PositionY) <= 3)
                    {
                        for (int j = i + 1; j < Math.Min(phasePackets.Count, i + 20); j++)
                        {
                            var packet = phasePackets[j];

                            if (InPacketParser.CanParse(packet))
                            {
                                var inPacket = InPacketParser.Parse(packet);
                                if (inPacket.EntityType == EntityType.Monster)
                                {
                                    onDeath.SummonMonsters.Add(new SummonMonster
                                    {
                                        VNum = inPacket.VNum,
                                        PositionX = inPacket.PositionX,
                                        PositionY = inPacket.PositionY,
                                        Move = true,
                                        IsBonus = true,
                                        IsHostile = true,
                                        IsTarget = false,
                                        IsBoss = false,
                                        IsMeteorite = false,
                                        Damage = 0,
                                        NoticeRange = 0,
                                        HasDelay = 0
                                    });
                                    foundEvents = true;
                                }
                            }
                            else if (GpPacketParser.CanParse(packet))
                            {
                                var gp = GpPacketParser.Parse(packet);
                                if (gp.Type >= 2 && !processedPortals.Contains(gp.PortalId))
                                {
                                    onDeath.ChangePortalTypes.Add(new ChangePortalType
                                    {
                                        IdOnMap = gp.PortalId,
                                        Type = gp.Type
                                    });
                                    onDeath.RefreshMapItems.Add(new object());
                                    processedPortals.Add(gp.PortalId);
                                    foundEvents = true;
                                }
                            }
                            else if (MsgPacketParser.CanParse(packet))
                            {
                                var msg = MsgPacketParser.Parse(packet);
                                if (!processedMessages.Contains(msg.Message))
                                {
                                    onDeath.SendMessages.Add(new SendMessage
                                    {
                                        Value = msg.Message,
                                        Type = msg.Type
                                    });
                                    processedMessages.Add(msg.Message);
                                    foundEvents = true;
                                }
                            }
                            else if (NpcReqPacketParser.CanParse(packet))
                            {
                                var npcReq = NpcReqPacketParser.Parse(packet);
                                if (!processedDialogs.Contains(npcReq.DialogId))
                                {
                                    onDeath.NpcDialogs.Add(new ValueAttribute
                                    {
                                        Value = npcReq.DialogId.ToString()
                                    });
                                    processedDialogs.Add(npcReq.DialogId);
                                    foundEvents = true;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            return foundEvents ? onDeath : null;
        }

        private int FindAlternateVNum(InPacket objectPacket, List<string> packets, int startIndex)
        {
            for (int i = startIndex + 1; i < Math.Min(packets.Count, startIndex + 15); i++)
            {
                if (InPacketParser.CanParse(packets[i]))
                {
                    var inPacket = InPacketParser.Parse(packets[i]);
                    if (inPacket.EntityId == objectPacket.EntityId && inPacket.VNum != objectPacket.VNum)
                    {
                        return inPacket.VNum;
                    }
                }
            }

            return 1000; // Fallback générique sans hardcode spécifique
        }

        private void FinalizeModel()
        {
            _model.InstanceEvents.CreateMaps = _maps.Values.OrderBy(m => m.Map).ToList();
        }
    }
}