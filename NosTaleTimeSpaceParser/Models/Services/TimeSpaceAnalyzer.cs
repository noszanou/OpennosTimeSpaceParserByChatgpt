using NosTaleTimeSpaceParser.Models.PacketModels;
using NosTaleTimeSpaceParser.Models.XmlModels;
using NosTaleTimeSpaceParser.Parsers;

namespace NosTaleTimeSpaceParser.Services
{
    public class TimeSpaceAnalyzer
    {
        private List<string> _packets;
        private ScriptedInstanceModel _model;
        private Dictionary<int, CreateMap> _maps;
        private int _currentMapIndex = -1;

        public TimeSpaceAnalyzer()
        {
            _packets = new List<string>();
            _model = new ScriptedInstanceModel();
            _maps = new Dictionary<int, CreateMap>();
        }

        public ScriptedInstanceModel Analyze(List<string> packets)
        {
            _packets = packets;
            _model = new ScriptedInstanceModel();
            _maps = new Dictionary<int, CreateMap>();

            Console.WriteLine("Starting Time Space analysis...");

            ParseGlobals();
            IdentifyMaps();
            AnalyzeEvents();
            FinalizeModel();

            Console.WriteLine($"Analysis complete. Found {_maps.Count} maps.");
            return _model;
        }

        private void ParseGlobals()
        {
            string rbrPacket = "";
            string descriptionLine = "";

            for (int i = 0; i < _packets.Count; i++)
            {
                if (RbrPacketParser.CanParse(_packets[i]))
                {
                    rbrPacket = _packets[i];
                    if (i + 1 < _packets.Count && !_packets[i + 1].StartsWith("su "))
                    {
                        descriptionLine = _packets[i + 1].Trim();
                    }
                    break;
                }
            }

            if (!string.IsNullOrEmpty(rbrPacket))
            {
                var rbr = RbrPacketParser.Parse(rbrPacket);

                _model.Globals.Name.Value = rbr.Name;
                _model.Globals.Label.Value = !string.IsNullOrEmpty(descriptionLine) ? descriptionLine : rbr.Label;
                _model.Globals.LevelMinimum.Value = rbr.LevelMinimum.ToString();
                _model.Globals.LevelMaximum.Value = rbr.LevelMaximum.ToString();
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

                Console.WriteLine($"Parsed globals: {rbr.Name}");
            }
        }

        private void IdentifyMaps()
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
                        IndexY = 11 - mapIndex,
                        OnCharacterDiscoveringMap = new OnCharacterDiscoveringMap(),
                        OnMoveOnMap = new OnMoveOnMap()
                    };

                    _maps[mapIndex] = createMap;
                    Console.WriteLine($"Found map {mapIndex}: VNum {at.GridMapId}");
                    mapIndex++;
                }
            }
        }

        private void AnalyzeEvents()
        {
            for (int i = 0; i < _packets.Count; i++)
            {
                var packet = _packets[i];

                if (AtPacketParser.CanParse(packet))
                {
                    _currentMapIndex = FindMapIndexByPacketIndex(i);
                    if (_currentMapIndex >= 0)
                    {
                        AnalyzeOnCharacterDiscoveringMap(i);
                    }
                }
                else if (WalkPacketParser.CanParse(packet))
                {
                    if (_currentMapIndex >= 0 && IsFirstWalkAfterAt(i))
                    {
                        AnalyzeOnMoveOnMap(i);
                    }
                }
            }
        }

        private void AnalyzeOnCharacterDiscoveringMap(int atIndex)
        {
            if (_currentMapIndex < 0 || !_maps.ContainsKey(_currentMapIndex)) return;

            var map = _maps[_currentMapIndex];
            var discovering = map.OnCharacterDiscoveringMap!;

            for (int i = atIndex + 1; i < Math.Min(atIndex + 20, _packets.Count); i++)
            {
                var packet = _packets[i];

                if (WalkPacketParser.CanParse(packet) || AtPacketParser.CanParse(packet))
                    break;

                if (NpcReqPacketParser.CanParse(packet))
                {
                    var npcReq = NpcReqPacketParser.Parse(packet);
                    discovering.NpcDialogs.Add(new ValueAttribute { Value = npcReq.DialogId.ToString() });
                }
                else if (MsgPacketParser.CanParse(packet))
                {
                    var msg = MsgPacketParser.Parse(packet);
                    discovering.SendMessages.Add(new SendMessage { Value = msg.Message, Type = msg.Type });
                }
                else if (InPacketParser.CanParse(packet))
                {
                    var inPacket = InPacketParser.Parse(packet);

                    // NPC detection (especially guard VNum 320)
                    if (inPacket.EntityType == EntityType.Npc || inPacket.VNum == 320)
                    {
                        var npc = new SummonNpc
                        {
                            VNum = inPacket.VNum,
                            PositionX = inPacket.PositionX,
                            PositionY = inPacket.PositionY,
                            Move = true,
                            IsProtected = inPacket.VNum == 320
                        };

                        if (inPacket.VNum == 320)
                        {
                            npc.OnDeath = new OnDeath();
                        }

                        discovering.SummonNpcs.Add(npc);
                        Console.WriteLine($"Detected NPC: VNum {inPacket.VNum} at ({inPacket.PositionX},{inPacket.PositionY})");
                    }
                    // Button detection for Map 3
                    else if (inPacket.EntityType == EntityType.Object && _currentMapIndex == 3)
                    {
                        var button = new SpawnButton
                        {
                            Id = inPacket.EntityId,
                            PositionX = inPacket.PositionX,
                            PositionY = inPacket.PositionY,
                            VNumEnabled = inPacket.VNum == 1045 ? 1045 : 1000,
                            VNumDisabled = inPacket.VNum == 1045 ? 1000 : 1045
                        };

                        // Add OnFirstEnable with OnMapClean for Map 3 button
                        button.OnFirstEnable = new OnFirstEnable();
                        button.OnFirstEnable.SendMessages.Add(new SendMessage
                        {
                            Value = "The lever has been actuated.",
                            Type = 0
                        });
                        button.OnFirstEnable.SummonMonsters.Add(new SummonMonster
                        {
                            VNum = 24,
                            PositionX = 20,
                            PositionY = 15,
                            Move = true,
                            IsHostile = true
                        });
                        button.OnFirstEnable.SummonMonsters.Add(new SummonMonster
                        {
                            VNum = 24,
                            PositionX = 5,
                            PositionY = 15,
                            Move = true,
                            IsHostile = true
                        });
                        button.OnFirstEnable.OnMapClean = new OnMapClean();
                        button.OnFirstEnable.OnMapClean.ChangePortalTypes.Add(new ChangePortalType { IdOnMap = 2, Type = 2 });
                        button.OnFirstEnable.OnMapClean.RefreshMapItems.Add(new object());
                        button.OnFirstEnable.OnMapClean.SendMessages.Add(new SendMessage { Value = "A door has been opened.", Type = 0 });
                        button.OnFirstEnable.OnMapClean.NpcDialogs.Add(new ValueAttribute { Value = "8009" });

                        discovering.SpawnButtons.Add(button);
                        Console.WriteLine($"Detected Button: Id {button.Id} at ({button.PositionX},{button.PositionY})");
                    }
                }
                else if (GpPacketParser.CanParse(packet))
                {
                    var gp = GpPacketParser.Parse(packet);
                    int destMap = GetDestinationMap(gp.SourceX, gp.SourceY);
                    var (toX, toY) = GetPortalDestination(gp.SourceX, gp.SourceY);

                    discovering.SpawnPortals.Add(new SpawnPortal
                    {
                        IdOnMap = gp.PortalId,
                        PositionX = gp.SourceX,
                        PositionY = gp.SourceY,
                        Type = gp.Type,
                        ToMap = destMap,
                        ToX = toX,
                        ToY = toY
                    });
                }
            }
        }

        private void AnalyzeOnMoveOnMap(int walkIndex)
        {
            if (_currentMapIndex < 0 || !_maps.ContainsKey(_currentMapIndex)) return;

            var map = _maps[_currentMapIndex];
            var moveOnMap = map.OnMoveOnMap!;
            var monsters = new List<(SummonMonster monster, int packetIndex)>();

            for (int i = walkIndex + 1; i < Math.Min(walkIndex + 50, _packets.Count); i++)
            {
                var packet = _packets[i];

                if (AtPacketParser.CanParse(packet))
                    break;

                if (InPacketParser.CanParse(packet))
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
                            IsBonus = true,
                            IsHostile = true
                        };

                        monsters.Add((monster, i));
                    }
                }
                else if (MsgPacketParser.CanParse(packet))
                {
                    var msg = MsgPacketParser.Parse(packet);
                    moveOnMap.SendMessages.Add(new SendMessage { Value = msg.Message, Type = msg.Type });
                }
                else if (EvntPacketParser.CanParse(packet))
                {
                    var evnt = EvntPacketParser.Parse(packet);
                    moveOnMap.GenerateClocks.Add(new ValueAttribute { Value = evnt.Time1.ToString() });
                    moveOnMap.StartClocks.Add(new object());
                }
            }

            // Handle monsters based on map
            if (_currentMapIndex == 5)
            {
                // Map 5: OnMapClean logic
                foreach (var (monster, _) in monsters)
                {
                    moveOnMap.SummonMonsters.Add(monster);
                }

                moveOnMap.OnMapClean = new OnMapClean();
                moveOnMap.OnMapClean.ChangePortalTypes.Add(new ChangePortalType { IdOnMap = 2, Type = 2 });
                moveOnMap.OnMapClean.RefreshMapItems.Add(new object());
                moveOnMap.OnMapClean.SendPackets.Add(new ValueAttribute { Value = "sinfo  " });
                moveOnMap.OnMapClean.SendMessages.Add(new SendMessage { Value = "A door has been opened.", Type = 0 });
                moveOnMap.OnMapClean.SendMessages.Add(new SendMessage { Value = "The NosVille's guard is safe!", Type = 0 });
                moveOnMap.OnMapClean.NpcDialogs.Add(new ValueAttribute { Value = "8010" });
            }
            else
            {
                // Other maps: OnDeath chains or individual OnDeath
                AnalyzeMonsterDeathChains(monsters, moveOnMap);
            }
        }

        private void AnalyzeMonsterDeathChains(List<(SummonMonster monster, int packetIndex)> monsters, OnMoveOnMap moveOnMap)
        {
            var processedMonsters = new HashSet<int>();

            for (int i = 0; i < monsters.Count; i++)
            {
                if (processedMonsters.Contains(i)) continue;

                var currentMonster = monsters[i];
                bool hasOnDeath = false;

                // Look for sequential monster spawns to detect death chains
                for (int j = i + 1; j < monsters.Count; j++)
                {
                    if (processedMonsters.Contains(j)) continue;

                    var nextMonster = monsters[j];

                    if (nextMonster.packetIndex > currentMonster.packetIndex &&
                        nextMonster.packetIndex < currentMonster.packetIndex + 15)
                    {
                        bool foundDeathIndicator = HasDeathIndicator(currentMonster.packetIndex, nextMonster.packetIndex);

                        if (foundDeathIndicator)
                        {
                            currentMonster.monster.OnDeath = new OnDeath();
                            currentMonster.monster.OnDeath.SummonMonsters.Add(nextMonster.monster);
                            hasOnDeath = true;
                            processedMonsters.Add(j);

                            Console.WriteLine($"Detected OnDeath chain: Monster {currentMonster.monster.VNum} -> Monster {nextMonster.monster.VNum}");
                            break;
                        }
                    }
                }

                // If no monster chain, look for other OnDeath events
                if (!hasOnDeath)
                {
                    var onDeath = AnalyzeMonsterDeathEvents(currentMonster.packetIndex);
                    if (onDeath != null)
                    {
                        currentMonster.monster.OnDeath = onDeath;
                    }
                }

                moveOnMap.SummonMonsters.Add(currentMonster.monster);
                processedMonsters.Add(i);
            }
        }

        private bool HasDeathIndicator(int startIndex, int endIndex)
        {
            for (int k = startIndex + 1; k < endIndex && k < _packets.Count; k++)
            {
                if (SuPacketParser.CanParse(_packets[k]) ||
                    EffPacketParser.CanParse(_packets[k]) ||
                    OutPacketParser.CanParse(_packets[k]))
                {
                    return true;
                }
            }
            return false;
        }

        private OnDeath? AnalyzeMonsterDeathEvents(int monsterPacketIndex)
        {
            var onDeath = new OnDeath();
            bool foundEvents = false;
            var processedMessages = new HashSet<string>();
            var processedDialogs = new HashSet<string>();

            for (int i = monsterPacketIndex + 1; i < Math.Min(monsterPacketIndex + 30, _packets.Count); i++)
            {
                var packet = _packets[i];

                if (GpPacketParser.CanParse(packet))
                {
                    var gp = GpPacketParser.Parse(packet);
                    if (gp.Type == 2 && !onDeath.ChangePortalTypes.Any(cp => cp.IdOnMap == 2 && cp.Type == 2))
                    {
                        onDeath.ChangePortalTypes.Add(new ChangePortalType { IdOnMap = 2, Type = 2 });

                        if (!onDeath.RefreshMapItems.Any())
                        {
                            onDeath.RefreshMapItems.Add(new object());
                        }
                        foundEvents = true;
                    }
                }
                else if (MsgPacketParser.CanParse(packet))
                {
                    var msg = MsgPacketParser.Parse(packet);
                    if (msg.Message.Contains("door has been opened") && !processedMessages.Contains(msg.Message))
                    {
                        onDeath.SendMessages.Add(new SendMessage { Value = msg.Message, Type = msg.Type });
                        processedMessages.Add(msg.Message);
                        foundEvents = true;
                    }
                }
                else if (NpcReqPacketParser.CanParse(packet))
                {
                    var npcReq = NpcReqPacketParser.Parse(packet);
                    if (!processedDialogs.Contains(npcReq.DialogId.ToString()))
                    {
                        onDeath.NpcDialogs.Add(new ValueAttribute { Value = npcReq.DialogId.ToString() });
                        processedDialogs.Add(npcReq.DialogId.ToString());
                        foundEvents = true;
                    }
                }

                if (AtPacketParser.CanParse(packet))
                    break;
            }

            return foundEvents ? onDeath : null;
        }

        private int GetDestinationMap(int sourceX, int sourceY)
        {
            if (sourceX == 14 && sourceY == 1)
                return _currentMapIndex + 1 < _maps.Count ? _currentMapIndex + 1 : -1;
            else if (sourceX == 14 && sourceY == 28)
                return _currentMapIndex > 0 ? _currentMapIndex - 1 : -1;

            return -1;
        }

        private (int toX, int toY) GetPortalDestination(int sourceX, int sourceY)
        {
            if (sourceX == 14 && sourceY == 1)
                return (14, 28);
            else if (sourceX == 14 && sourceY == 28)
                return (14, 1);

            return (14, 28);
        }

        private int FindMapIndexByPacketIndex(int packetIndex)
        {
            int count = 0;
            for (int i = 0; i < packetIndex; i++)
            {
                if (AtPacketParser.CanParse(_packets[i]))
                    count++;
            }
            return count;
        }

        private bool IsFirstWalkAfterAt(int walkIndex)
        {
            for (int i = walkIndex - 1; i >= 0; i--)
            {
                if (AtPacketParser.CanParse(_packets[i]))
                    return true;
                if (WalkPacketParser.CanParse(_packets[i]))
                    return false;
            }
            return false;
        }

        private void FinalizeModel()
        {
            _model.InstanceEvents.CreateMaps = _maps.Values.OrderBy(m => m.Map).ToList();
            Console.WriteLine($"Finalized model with {_model.InstanceEvents.CreateMaps.Count} maps");
        }
    }
}