using NosTaleTimeSpaceParser.Models.PacketModels;
using NosTaleTimeSpaceParser.Parsers;
using ScriptedInstanceModel.Models.ScriptedInstance;
using ScriptedInstanceModel.Objects;
using ScriptedInstanceModel.Events;

namespace NosTaleTimeSpaceParser.Services
{
    public class TimeSpaceAnalyzer
    {
        private List<string> _packets;
        private ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel _model;
        private Dictionary<int, CreateMap> _maps;
        private Dictionary<int, List<string>> _mapPackets;
        private string? _descriptionLine;

        public TimeSpaceAnalyzer()
        {
            _packets = new List<string>();
            _model = new ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel();
            _maps = new Dictionary<int, CreateMap>();
            _mapPackets = new Dictionary<int, List<string>>();
        }

        public ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel Analyze(List<string> packets)
        {
            _packets = packets;
            _model = new ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel();
            _model.Globals = new Globals();
            _model.InstanceEvents = new InstanceEvent();
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

                    _model.Globals.Name = new Name { Value = rbr.Name };
                    _model.Globals.Label = new Label { Value = !string.IsNullOrEmpty(_descriptionLine) ? _descriptionLine : rbr.Label };
                    _model.Globals.LevelMinimum = new Level { Value = (byte)rbr.LevelMinimum };
                    _model.Globals.LevelMaximum = new Level { Value = (byte)rbr.LevelMaximum };
                    _model.Globals.Lives = new Lives { Value = 1 };

                    var drawItems = new List<Item>();
                    foreach (var item in rbr.DrawItems)
                    {
                        drawItems.Add(new Item
                        {
                            VNum = (short)item.VNum,
                            Amount = (short)item.Amount
                        });
                    }
                    _model.Globals.DrawItems = drawItems.ToArray();

                    var specialItems = new List<Item>();
                    foreach (var item in rbr.SpecialItems)
                    {
                        specialItems.Add(new Item
                        {
                            VNum = (short)item.VNum,
                            Amount = (short)item.Amount
                        });
                    }
                    _model.Globals.SpecialItems = specialItems.ToArray();

                    var giftItems = new List<Item>();
                    foreach (var item in rbr.GiftItems)
                    {
                        giftItems.Add(new Item
                        {
                            VNum = (short)item.VNum,
                            Amount = (short)item.Amount
                        });
                    }
                    _model.Globals.GiftItems = giftItems.ToArray();

                    _model.Globals.Gold = new Gold { Value = 1500 };
                    _model.Globals.Reputation = new Reputation { Value = 50 };

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
                        VNum = (short)at.GridMapId,
                        IndexX = 3,
                        IndexY = (byte)(11 - mapIndex)
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
            var discoveringPackets = new List<string>();
            var moveOnMapPackets = new List<string>();
            bool inMovePhase = false;

            if (mapIndex > 0)
            {
                ProcessPortalsDirectly(map, packets, mapIndex);
            }

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

            map.OnCharacterDiscoveringMap = new OnCharacterDiscoveringMap();

            if (mapIndex == 0)
            {
                ProcessPortalsForDiscovering(map.OnCharacterDiscoveringMap, packets, mapIndex);
            }

            if (discoveringPackets.Any())
            {
                ProcessDiscoveringPhase(map.OnCharacterDiscoveringMap, discoveringPackets, packets, mapIndex);
            }

            ProcessButtons(map, packets);

            if (moveOnMapPackets.Any())
            {
                var onMoveOnMapList = new List<OnMoveOnMap>();
                var onMoveOnMap = new OnMoveOnMap();
                ProcessMovePhase(onMoveOnMap, moveOnMapPackets, packets, mapIndex);
                onMoveOnMapList.Add(onMoveOnMap);
                map.OnMoveOnMap = onMoveOnMapList.ToArray();
            }
        }

        private void ProcessPortalsDirectly(CreateMap map, List<string> packets, int mapIndex)
        {
            var processedPortals = new HashSet<string>();
            var portalsList = new List<SpawnPortal>();

            foreach (var packet in packets)
            {
                if (GpPacketParser.CanParse(packet))
                {
                    var gp = GpPacketParser.Parse(packet);
                    var portalKey = $"{gp.PortalId}_{gp.SourceX}_{gp.SourceY}";

                    if (processedPortals.Contains(portalKey))
                        continue;

                    processedPortals.Add(portalKey);

                    byte idOnMap = gp.SourceY == 1 ? (byte)0 : (byte)2;

                    int toMap = CalculateToMap(gp, mapIndex);
                    int toX = gp.SourceX;
                    int toY = gp.SourceY == 1 ? 28 : 1;

                    var portal = new SpawnPortal
                    {
                        IdOnMap = idOnMap,
                        PositionX = (short)gp.SourceX,
                        PositionY = (short)gp.SourceY,
                        Type = (short)gp.Type,
                        ToMap = (short)toMap,
                        ToX = (short)toX,
                        ToY = (short)toY
                    };

                    if (gp.Type == 5)
                    {
                        portal.ToMap = -1;
                        portal.OnTraversal = new OnTraversal();
                        portal.OnTraversal.End = new End { Type = 5 };
                    }

                    portalsList.Add(portal);
                }
            }

            map.SpawnPortal = portalsList.OrderBy(p => p.IdOnMap).ToArray();
        }

        private void ProcessPortalsForDiscovering(OnCharacterDiscoveringMap discovering, List<string> packets, int mapIndex)
        {
            var processedPortals = new HashSet<string>();
            var portalsList = new List<SpawnPortal>();

            foreach (var packet in packets)
            {
                if (GpPacketParser.CanParse(packet))
                {
                    var gp = GpPacketParser.Parse(packet);
                    var portalKey = $"{gp.PortalId}_{gp.SourceX}_{gp.SourceY}";

                    if (processedPortals.Contains(portalKey))
                        continue;

                    processedPortals.Add(portalKey);

                    byte idOnMap = gp.SourceY == 1 ? (byte)0 : (byte)2;

                    int toMap = CalculateToMap(gp, mapIndex);
                    int toX = gp.SourceX;
                    int toY = gp.SourceY == 1 ? 28 : 1;

                    var portal = new SpawnPortal
                    {
                        IdOnMap = idOnMap,
                        PositionX = (short)gp.SourceX,
                        PositionY = (short)gp.SourceY,
                        Type = (short)gp.Type,
                        ToMap = (short)toMap,
                        ToX = (short)toX,
                        ToY = (short)toY
                    };

                    if (gp.Type == 5)
                    {
                        portal.ToMap = -1;
                        portal.OnTraversal = new OnTraversal();
                        portal.OnTraversal.End = new End { Type = 5 };
                    }

                    portalsList.Add(portal);
                }
            }

            discovering.SpawnPortal = portalsList.OrderBy(p => p.IdOnMap).ToArray();
        }

        private void ProcessButtons(CreateMap map, List<string> packets)
        {
            var spawnButtons = new List<SpawnButton>();
            var processedButtons = new HashSet<int>();

            for (int i = 0; i < packets.Count; i++)
            {
                var packet = packets[i];

                if (InPacketParser.CanParse(packet))
                {
                    var inPacket = InPacketParser.Parse(packet);

                    if (inPacket.EntityType == EntityType.Object && !processedButtons.Contains(inPacket.EntityId))
                    {
                        processedButtons.Add(inPacket.EntityId);

                        var button = new SpawnButton
                        {
                            Id = inPacket.EntityId,
                            PositionX = (short)inPacket.PositionX,
                            PositionY = (short)inPacket.PositionY,
                            VNumEnabled = (short)inPacket.VNum,
                            VNumDisabled = (short)FindAlternateVNum(inPacket, packets, i)
                        };

                        button.OnFirstEnable = new OnFirstEnable();
                        spawnButtons.Add(button);
                    }
                }
            }

            map.SpawnButton = spawnButtons.ToArray();
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
            var sendMessages = new List<SendMessage>();
            var npcDialogs = new List<NpcDialog>();
            var sendPackets = new List<SendPacket>();
            var summonNpcs = new List<SummonNpc>();

            for (int i = 0; i < packets.Count; i++)
            {
                var packet = packets[i];

                if (MsgPacketParser.CanParse(packet))
                {
                    var msg = MsgPacketParser.Parse(packet);
                    sendMessages.Add(new SendMessage
                    {
                        Value = msg.Message,
                        Type = (byte)msg.Type
                    });
                }
                else if (NpcReqPacketParser.CanParse(packet))
                {
                    var npcReq = NpcReqPacketParser.Parse(packet);
                    npcDialogs.Add(new NpcDialog { Value = npcReq.DialogId });
                }
                else if (packet.Trim().StartsWith("sinfo ") || packet.Trim().StartsWith("rsfm "))
                {
                    sendPackets.Add(new SendPacket { Value = packet.Trim() });
                }
                else if (InPacketParser.CanParse(packet))
                {
                    var inPacket = InPacketParser.Parse(packet);

                    if (inPacket.EntityType == EntityType.Npc)
                    {
                        var npc = new SummonNpc
                        {
                            VNum = (short)inPacket.VNum,
                            PositionX = (short)inPacket.PositionX,
                            PositionY = (short)inPacket.PositionY,
                            Move = true,
                            IsProtected = DetermineIsProtected(inPacket, allPackets, packets, i)
                        };

                        var onDeath = DetectNpcOnDeath(inPacket, allPackets, packets, i);
                        if (onDeath != null)
                        {
                            npc.OnDeath = onDeath;
                        }

                        summonNpcs.Add(npc);
                    }
                }
            }

            discovering.SendMessage = sendMessages.ToArray();
            discovering.NpcDialog = npcDialogs.ToArray();
            discovering.SendPacket = sendPackets.ToArray();
            discovering.SummonNpc = summonNpcs.ToArray();
        }

        private void ProcessMovePhase(OnMoveOnMap moveOnMap, List<string> packets, List<string> allPackets, int mapIndex)
        {
            var sendMessages = new List<SendMessage>();
            var sendPackets = new List<SendPacket>();
            var generateClocks = new List<GenerateClock>();
            var startClocks = new List<StartClock>();
            var summonMonsters = new List<SummonMonster>();
            var summonNpcs = new List<SummonNpc>();

            for (int i = 0; i < packets.Count; i++)
            {
                var packet = packets[i];

                if (MsgPacketParser.CanParse(packet))
                {
                    var msg = MsgPacketParser.Parse(packet);
                    sendMessages.Add(new SendMessage
                    {
                        Value = msg.Message,
                        Type = (byte)msg.Type
                    });
                }
                else if (packet.Trim().StartsWith("sinfo ") || packet.Trim().StartsWith("rsfm "))
                {
                    sendPackets.Add(new SendPacket { Value = packet.Trim() });
                }
                else if (EvntPacketParser.CanParse(packet))
                {
                    var evnt = EvntPacketParser.Parse(packet);
                    generateClocks.Add(new GenerateClock { Value = evnt.Time1 });
                    startClocks.Add(new StartClock());
                }
                else if (InPacketParser.CanParse(packet))
                {
                    var inPacket = InPacketParser.Parse(packet);

                    if (inPacket.EntityType == EntityType.Monster)
                    {
                        var monster = new SummonMonster
                        {
                            VNum = (short)inPacket.VNum,
                            PositionX = (short)inPacket.PositionX,
                            PositionY = (short)inPacket.PositionY,
                            Move = true,
                            IsBonus = DetermineIsBonus(inPacket, allPackets, packets, i),
                            IsHostile = DetermineIsHostile(inPacket, allPackets, packets, i),
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

                        summonMonsters.Add(monster);
                    }
                    else if (inPacket.EntityType == EntityType.Npc)
                    {
                        var npc = new SummonNpc
                        {
                            VNum = (short)inPacket.VNum,
                            PositionX = (short)inPacket.PositionX,
                            PositionY = (short)inPacket.PositionY,
                            Move = true,
                            IsProtected = DetermineIsProtected(inPacket, allPackets, packets, i)
                        };

                        var onDeath = DetectNpcOnDeath(inPacket, allPackets, packets, i);
                        if (onDeath != null)
                        {
                            npc.OnDeath = onDeath;
                        }

                        summonNpcs.Add(npc);
                    }
                }
            }

            moveOnMap.SendMessage = sendMessages.ToArray();
            moveOnMap.SendPacket = sendPackets.ToArray();
            moveOnMap.GenerateClock = generateClocks.FirstOrDefault();
            moveOnMap.StartClock = startClocks.FirstOrDefault();
            moveOnMap.SummonMonster = summonMonsters.ToArray();
            moveOnMap.SummonNpc = summonNpcs.ToArray();

            if (generateClocks.Count > 1)
            {
                moveOnMap.GenerateMapClock = new GenerateMapClock { Value = generateClocks[1].Value };
                moveOnMap.StartMapClock = startClocks.Count > 1 ? startClocks[1] : new StartClock();
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
                onDeath.End = new End { Type = 2 };
                return onDeath;
            }

            return null;
        }

        private bool DetermineIsBonus(InPacket monsterPacket, List<string> allPackets, List<string> phasePackets, int index)
        {
            // Un monstre est bonus si :
            // 1. Il n'y a pas de combat immédiat (pas de SU packet qui le cible)
            // 2. Il spawn au début sans contexte de combat

            bool hasCombatContext = false;

            // Vérifier s'il y a du combat après le spawn
            for (int i = index; i < Math.Min(phasePackets.Count, index + 5); i++)
            {
                if (SuPacketParser.CanParse(phasePackets[i]))
                {
                    var su = SuPacketParser.Parse(phasePackets[i]);
                    if (su.TargetId == monsterPacket.EntityId)
                    {
                        hasCombatContext = true;
                        break;
                    }
                }
            }

            // Si pas de combat et pas dans un contexte spécial (bouton, etc), c'est bonus
            return !hasCombatContext;
        }

        private bool DetermineIsHostile(InPacket monsterPacket, List<string> allPackets, List<string> phasePackets, int index)
        {
            // Un monstre est hostile si :
            // 1. Il y a un message d'attaque autour
            // 2. Il est dans un contexte de bouton/lever
            // 3. Il y a un contexte de combat clair

            // Vérifier le contexte autour
            for (int i = Math.Max(0, index - 10); i < Math.Min(phasePackets.Count, index + 10); i++)
            {
                var packet = phasePackets[i];

                // Messages d'attaque
                if (packet.ToLower().Contains("attack") ||
                    packet.ToLower().Contains("defeat") ||
                    packet.ToLower().Contains("kill") ||
                    packet.ToLower().Contains("enemies"))
                {
                    return true;
                }

                // Contexte de bouton/lever
                if (packet.ToLower().Contains("lever") ||
                    packet.ToLower().Contains("actiated"))
                {
                    return true;
                }
            }

            return false;
        }

        private OnDeath? DetectMonsterOnDeath(InPacket monsterPacket, List<string> allPackets, List<string> phasePackets, int index)
        {
            var processedPortals = new HashSet<int>();
            var processedMessages = new HashSet<string>();
            var processedDialogs = new HashSet<int>();
            bool foundEvents = false;
            var onDeath = new OnDeath();

            var summonMonsters = new List<SummonMonster>();
            var changePortalTypes = new List<ChangePortalType>();
            var sendMessages = new List<SendMessage>();
            var npcDialogs = new List<NpcDialog>();

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
                                    summonMonsters.Add(new SummonMonster
                                    {
                                        VNum = (short)inPacket.VNum,
                                        PositionX = (short)inPacket.PositionX,
                                        PositionY = (short)inPacket.PositionY,
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
                                    changePortalTypes.Add(new ChangePortalType
                                    {
                                        IdOnMap = gp.PortalId,
                                        Type = (sbyte)gp.Type
                                    });
                                    processedPortals.Add(gp.PortalId);
                                    foundEvents = true;
                                }
                            }
                            else if (MsgPacketParser.CanParse(packet))
                            {
                                var msg = MsgPacketParser.Parse(packet);
                                if (!processedMessages.Contains(msg.Message))
                                {
                                    sendMessages.Add(new SendMessage
                                    {
                                        Value = msg.Message,
                                        Type = (byte)msg.Type
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
                                    npcDialogs.Add(new NpcDialog
                                    {
                                        Value = npcReq.DialogId
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

            if (foundEvents)
            {
                onDeath.SummonMonster = summonMonsters.ToArray();
                onDeath.ChangePortalType = changePortalTypes.ToArray();
                onDeath.SendMessage = sendMessages.ToArray();
                onDeath.NpcDialog = npcDialogs.ToArray();
                onDeath.RefreshMapItems = changePortalTypes.Any() ? new object() : null;
                return onDeath;
            }

            return null;
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

            return objectPacket.VNum + 45;
        }

        private void FinalizeModel()
        {
            _model.InstanceEvents.CreateMap = _maps.Values.OrderBy(m => m.Map).ToArray();
        }
    }
}