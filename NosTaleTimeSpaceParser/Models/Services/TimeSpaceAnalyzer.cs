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
        private Dictionary<int, TimeSpaceMap> _maps;
        private List<TimeSpaceMonster> _mapMonsters;
        private List<TimeSpaceMonster> _allMonsters;

        // États comme dans le vieux tool
        private bool _flag1; // Phase de découverte
        private bool _flag2; // Clock flag
        private bool _flag3; // Clear monster flag
        private int _num2;   // Pour les calculs de temps
        private int _num3;   // Compteur walk

        // Variables de contexte
        private TimeSpaceMap _currentMap;
        private TimeSpaceMonster _targetMonster;
        private object _currentTarget;
        private string _currentEventName;
        private short _posX, _posY;
        private short _indexX, _indexY;

        // IDs dynamiques (détectés automatiquement)
        private int _playerId = -1;  // Détecté depuis le premier packet AT
        private readonly HashSet<int> _mateIds = new HashSet<int>();  // Détectés automatiquement

        public TimeSpaceAnalyzer()
        {
            _packets = new List<string>();
            _model = new ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel();
            _maps = new Dictionary<int, TimeSpaceMap>();
            _mapMonsters = new List<TimeSpaceMonster>();
            _allMonsters = new List<TimeSpaceMonster>();
        }

        public ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel Analyze(List<string> packets)
        {
            _packets = packets;
            InitializeModel();
            ParseGlobalsFromRbr();
            ProcessPacketsSequentially();
            GenerateXmlStructure();
            return _model;
        }

        private void InitializeModel()
        {
            _model = new ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel();
            _model.Globals = new Globals();
            _model.InstanceEvents = new InstanceEvent();
            _maps = new Dictionary<int, TimeSpaceMap>();
            _mapMonsters = new List<TimeSpaceMonster>();
            _allMonsters = new List<TimeSpaceMonster>();
        }

        private void ParseGlobalsFromRbr()
        {
            string descriptionLine = null;

            for (int i = 0; i < _packets.Count; i++)
            {
                if (RbrPacketParser.CanParse(_packets[i]))
                {
                    var rbr = RbrPacketParser.Parse(_packets[i]);

                    // Extraire le vrai nom du titre (après les scores)
                    string realName = ExtractRealNameFromRbr(rbr.Name);

                    // Chercher la description sur la ligne suivante
                    if (i + 1 < _packets.Count)
                    {
                        var nextLine = _packets[i + 1].Trim();
                        if (!IsPacketLine(nextLine) && !string.IsNullOrEmpty(nextLine))
                        {
                            descriptionLine = nextLine;
                        }
                    }

                    _model.Globals.Name = new Name { Value = realName };
                    _model.Globals.Label = new Label { Value = descriptionLine ?? rbr.Label };
                    _model.Globals.LevelMinimum = new Level { Value = (byte)rbr.LevelMinimum };
                    _model.Globals.LevelMaximum = new Level { Value = (byte)rbr.LevelMaximum };
                    _model.Globals.Lives = new Lives { Value = 1 };

                    // Items dans l'ordre exact de Untitled-1.xml
                    _model.Globals.DrawItems = rbr.DrawItems.Select(item => new Item
                    {
                        VNum = (short)item.VNum,
                        Amount = (short)item.Amount
                    }).ToArray();

                    _model.Globals.SpecialItems = rbr.SpecialItems.Select(item => new Item
                    {
                        VNum = (short)item.VNum,
                        Amount = (short)item.Amount
                    }).ToArray();

                    _model.Globals.GiftItems = rbr.GiftItems.Select(item => new Item
                    {
                        VNum = (short)item.VNum,
                        Amount = (short)item.Amount
                    }).ToArray();

                    _model.Globals.Gold = new Gold { Value = 1500 };
                    _model.Globals.Reputation = new Reputation { Value = 50 };
                    break;
                }
            }
        }

        private string ExtractRealNameFromRbr(string fullName)
        {
            // Format: "1153.Title1 0 0 Time-Space Tutorial"
            // Extraire: "Time-Space Tutorial"
            var parts = fullName.Split(' ');

            int nameStartIndex = -1;
            for (int i = 0; i < parts.Length; i++)
            {
                if (!IsNumericOrScore(parts[i]))
                {
                    nameStartIndex = i;
                    break;
                }
            }

            if (nameStartIndex > 0)
            {
                return string.Join(" ", parts.Skip(nameStartIndex));
            }

            return fullName;
        }

        private bool IsNumericOrScore(string part)
        {
            return part == "0" || part == "0." ||
                   int.TryParse(part, out _) ||
                   float.TryParse(part, out _) ||
                   part.Contains(".");
        }

        private bool IsPacketLine(string line)
        {
            var packetPrefixes = new[] { "at ", "in ", "gp ", "msg ", "walk ", "su ", "eff ", "evnt ", "npc_req ", "out ", "preq", "rsfn ", "rsfm ", "rsfp " };
            return packetPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private void ProcessPacketsSequentially()
        {
            for (int i = 0; i < _packets.Count; i++)
            {
                var packet = _packets[i];
                ProcessSinglePacket(packet);
            }
        }

        private void ProcessSinglePacket(string packet)
        {
            if (AtPacketParser.CanParse(packet))
            {
                HandleAtPacket(AtPacketParser.Parse(packet));
            }
            else if (RsfPacketParser.CanParse(packet))
            {
                HandleRsfnPacket(RsfPacketParser.Parse(packet));
            }
            else if (WalkPacketParser.CanParse(packet))
            {
                HandleWalkPacket(WalkPacketParser.Parse(packet));
            }
            else if (InPacketParser.CanParse(packet))
            {
                HandleInPacket(InPacketParser.Parse(packet));
            }
            else if (SuPacketParser.CanParse(packet))
            {
                HandleSuPacket(SuPacketParser.Parse(packet));
            }
            else if (GpPacketParser.CanParse(packet))
            {
                HandleGpPacket(GpPacketParser.Parse(packet));
            }
            else if (MsgPacketParser.CanParse(packet))
            {
                HandleMsgPacket(MsgPacketParser.Parse(packet));
            }
            else if (NpcReqPacketParser.CanParse(packet))
            {
                HandleNpcReqPacket(NpcReqPacketParser.Parse(packet));
            }
            else if (EvntPacketParser.CanParse(packet))
            {
                HandleEvntPacket(EvntPacketParser.Parse(packet));
            }
            else if (OutPacketParser.CanParse(packet))
            {
                HandleOutPacket(OutPacketParser.Parse(packet));
            }
            else if (PreqPacketParser.CanParse(packet))
            {
                HandlePreqPacket();
            }
            else if (EffPacketParser.CanParse(packet))
            {
                HandleEffPacket(EffPacketParser.Parse(packet));
            }
            else if (packet.Trim().StartsWith("mapclear") || packet.Trim().StartsWith("mapclean"))
            {
                HandleMapClear();
            }
            else if (packet.Trim().StartsWith("rsfm ") || packet.Trim().StartsWith("sinfo ") ||
                     packet.Trim().StartsWith("minfo ") || packet.Trim().StartsWith("msgi "))
            {
                HandleSendPacket(packet.Trim());
            }
        }

        private void HandleAtPacket(AtPacket packet)
        {
            // Détecter l'ID du joueur dynamiquement depuis le premier AT
            if (_playerId == -1)
            {
                _playerId = packet.MapId; // L'ID du joueur est dans MapId du packet AT
            }

            var mapId = _maps.Count;
            _currentMap = new TimeSpaceMap
            {
                Id = mapId,
                VNum = (short)packet.GridMapId,
                IndexX = _indexX,
                IndexY = _indexY,
                OnCharacterDiscoveringMap = new List<TimeSpaceEvent>(),
                OnMoveOnMap = new List<TimeSpaceEvent>(),
                OnMapClean = new List<TimeSpaceEvent>(),
                SpawnPortals = new List<TimeSpacePortal>(),
                SummonNpcs = new List<TimeSpaceNpc>(),
                SummonMonsters = new List<TimeSpaceMonster>(),
                SpawnButtons = new List<TimeSpaceButton>(),
                SendMessages = new List<TimeSpaceMessage>(),
                SendPackets = new List<string>(),
                NpcDialogs = new List<int>(),
                ProcessedPortals = new HashSet<string>(), // Éviter les doublons
                ProcessedButtons = new HashSet<int>()     // Éviter les doublons
            };

            _maps[mapId] = _currentMap;
            _mapMonsters.Clear();
            _flag1 = true;
            _flag2 = false;
            _flag3 = false;
            _num3 = 0;
            _currentTarget = _currentMap;
            _currentEventName = "OnCharacterDiscoveringMap";

            _posX = (short)packet.PositionX;
            _posY = (short)packet.PositionY;
        }

        private void HandleRsfnPacket(RsfPacket packet)
        {
            if (packet.Values.Count >= 2)
            {
                _indexX = (short)packet.Values[0];
                _indexY = (short)packet.Values[1];
            }
        }

        private void HandleWalkPacket(WalkPacket packet)
        {
            _posX = (short)packet.PositionX;
            _posY = (short)packet.PositionY;

            if (_num3 == 1)
            {
                _flag1 = false;
                // Passer en mode OnMoveOnMap
                _currentEventName = "OnMoveOnMap";
            }
            _num3++;
        }

        private void HandleInPacket(InPacket packet)
        {
            // Filtrage IMMÉDIAT des mates avant tout traitement
            if (packet.EntityType == EntityType.Npc &&
                (packet.VNum == 1548 || packet.VNum == 2640))
            {
                // C'est un mate → on l'ignore complètement
                return;
            }

            switch (packet.EntityType)
            {
                case EntityType.Npc:
                    HandleNpcSummon(packet);
                    break;
                case EntityType.Monster:
                    HandleMonsterSummon(packet);
                    break;
                case EntityType.Object:
                    HandleObjectSummon(packet);
                    break;
            }
        }

        private bool IsPlayerMate(InPacket packet)
        {
            // Filtrer les mates : VNums 1548 et 2640 avec Equipment contenant @
            if (packet.EntityType == EntityType.Npc &&
                (packet.VNum == 1548 || packet.VNum == 2640) &&
                packet.Equipment.Contains("@"))
            {
                return true;
            }
            return false;
        }

        private void HandleNpcSummon(InPacket packet)
        {
            var npc = new TimeSpaceNpc
            {
                VNum = (short)packet.VNum,
                EntityId = packet.EntityId,
                PositionX = (short)packet.PositionX,
                PositionY = (short)packet.PositionY,
                Move = true,
                IsProtected = DetermineIsProtected(packet)
            };

            // NPCs toujours dans OnCharacterDiscoveringMap (sauf si spawn dans OnDeath)
            if (_currentEventName == "OnCharacterDiscoveringMap")
            {
                _currentMap.SummonNpcs.Add(npc);
            }
            else
            {
                AddEventToCurrentTarget("SummonNpc", npc);
            }
        }

        private void HandleMonsterSummon(InPacket packet)
        {
            var monster = new TimeSpaceMonster
            {
                VNum = (short)packet.VNum,
                EntityId = packet.EntityId,
                PositionX = (short)packet.PositionX,
                PositionY = (short)packet.PositionY,
                IsDead = false,
                IsBonus = false,
                IsTarget = false,
                Move = true,
                IsHostile = false,
                OnDeathEvents = new List<TimeSpaceEvent>()
            };

            // Déterminer la phase selon la logique expliquée
            if (_currentEventName == "OnCharacterDiscoveringMap")
            {
                // Phase découverte (entre at et premier su)
                _mapMonsters.Add(monster);
                _allMonsters.Add(monster);
                _currentMap.SummonMonsters.Add(monster);
            }
            else if (_currentEventName == "OnDeath" || _currentEventName == "OnFirstEnable")
            {
                // Phase événement (après su ou activation levier)
                monster.IsBonus = true;
                monster.IsHostile = true;
                AddEventToCurrentTarget("SummonMonster", monster);
                _allMonsters.Add(monster);
            }
            else if (_currentEventName == "OnMapClean")
            {
                // Exception: si c'est OnMapClean, ajouter à la map
                _currentMap.SummonMonsters.Add(monster);
                _allMonsters.Add(monster);
            }
        }

        private bool DetermineIsProtected(InPacket npcPacket)
        {
            // Logique simple: NPC VNum 320 = garde protégé
            return npcPacket.VNum == 320;
        }

        private void HandleObjectSummon(InPacket packet)
        {
            // Éviter les doublons de boutons
            if (_currentMap.ProcessedButtons.Contains(packet.EntityId))
                return;

            _currentMap.ProcessedButtons.Add(packet.EntityId);

            var button = new TimeSpaceButton
            {
                Id = packet.EntityId,
                PositionX = (short)packet.PositionX,
                PositionY = (short)packet.PositionY,
                VNumEnabled = (short)packet.VNum,
                VNumDisabled = (short)(packet.VNum == 1045 ? 1000 : packet.VNum - 45),
                OnFirstEnable = new List<TimeSpaceEvent>()
            };

            _currentMap.SpawnButtons.Add(button);
        }

        private void HandleSuPacket(SuPacket packet)
        {
            // su = mort d'un monstre → déclenche OnDeath
            // Utiliser l'ID du joueur détecté dynamiquement
            if (packet.Type == 1 && packet.CallerId == _playerId) // Joueur attaque
            {
                var monster = _allMonsters.FirstOrDefault(m => m.EntityId == packet.TargetId);
                if (monster != null && !monster.IsDead)
                {
                    monster.IsDead = true;
                    _currentTarget = monster;
                    _currentEventName = "OnDeath";

                    // Les événements OnDeath (nouveau monstre, ChangePortalType, etc.) 
                    // vont être ajoutés par les packets suivants
                }
            }
        }

        private void HandleMapClear()
        {
            // mapclear = tous les monstres morts → déclenche OnMapClean
            foreach (var monster in _mapMonsters.Where(m => !m.IsDead))
            {
                monster.IsDead = true;
            }

            _currentTarget = _currentMap;
            _currentEventName = "OnMapClean";
        }

        private void HandleGpPacket(GpPacket packet)
        {
            var portalKey = $"{packet.PortalId}_{packet.SourceX}_{packet.SourceY}_{packet.Type}";

            // Éviter les doublons de portails
            if (_currentMap.ProcessedPortals.Contains(portalKey))
                return;

            _currentMap.ProcessedPortals.Add(portalKey);

            // Si c'est un changement de type de portail (Type >= 2), c'est un événement
            if (packet.Type >= 2)
            {
                var changeEvent = new TimeSpaceEvent
                {
                    Type = "ChangePortalType",
                    PortalId = packet.PortalId,
                    Value = packet.Type
                };

                AddEventToCurrentTarget("ChangePortalType", changeEvent);
                AddEventToCurrentTarget("RefreshMapItems", null);
                return; // Ne pas créer de portail physique
            }

            // Portail initial (Type 0 ou 1)  
            var portal = new TimeSpacePortal
            {
                IdOnMap = (byte)packet.PortalId,
                PositionX = (short)packet.SourceX,
                PositionY = (short)packet.SourceY,
                Type = (short)packet.Type,
                ToMap = (short)CalculateToMap(packet),
                ToX = (short)packet.SourceX,
                ToY = (short)(packet.SourceY == 1 ? 28 : 1),
                OnTraversalEvents = new List<TimeSpaceEvent>()
            };

            if (packet.Type == 5) // Portail de sortie
            {
                portal.ToMap = -1;
                portal.OnTraversalEvents.Add(new TimeSpaceEvent { Type = "End", Value = 5 });
            }

            // Ajouter seulement si pas déjà présent
            if (!_currentMap.SpawnPortals.Any(p => p.IdOnMap == portal.IdOnMap && p.PositionX == portal.PositionX && p.PositionY == portal.PositionY))
            {
                _currentMap.SpawnPortals.Add(portal);
            }
        }

        private int CalculateToMap(GpPacket gp)
        {
            var currentMapIndex = _maps.Count - 1;

            if (gp.Type == 5) return -1;

            if (gp.SourceY == 1) // Portail vers le haut
                return currentMapIndex + 1;
            else if (gp.SourceY == 28) // Portail vers le bas  
                return currentMapIndex - 1;

            return currentMapIndex;
        }

        private void HandleMsgPacket(MsgPacket packet)
        {
            var message = new TimeSpaceMessage
            {
                Value = packet.Message,
                Type = (byte)packet.Type
            };

            if (_flag1)
            {
                _currentMap.SendMessages.Add(message);
            }
            else
            {
                AddEventToCurrentTarget("SendMessage", message);
            }
        }

        private void HandleNpcReqPacket(NpcReqPacket packet)
        {
            if (_flag1)
            {
                _currentMap.NpcDialogs.Add(packet.DialogId);
            }
            else
            {
                AddEventToCurrentTarget("NpcDialog", packet.DialogId);
            }
        }

        private void HandleEvntPacket(EvntPacket packet)
        {
            switch (packet.Type)
            {
                case 1: // SimpleClock
                    if (packet.Time1 == packet.Time2 && _flag1)
                    {
                        _currentMap.GenerateClock = packet.Time1;
                        _currentMap.HasStartClock = true;
                    }
                    break;

                case 3: // MapClock - StartMapClock avec OnStop/OnTimeout
                    if (packet.Time1 == packet.Time2 && _flag1)
                    {
                        _currentMap.GenerateMapClock = packet.Time1;
                        _currentMap.HasStartMapClock = true;
                    }
                    break;
            }
        }

        private void HandleOutPacket(OutPacket packet)
        {
            if (packet.Type == 3) // Monster
            {
                if (!_flag3)
                {
                    _flag3 = true;
                    AddEventToCurrentTarget("ClearMonster", null);
                }
            }
        }

        private void HandlePreqPacket()
        {
            // PREQ = changement de map = tous les monstres de la map précédente sont morts
            // C'est ici qu'on détecte OnMapClean !
            if (_mapMonsters.Any(m => !m.IsDead))
            {
                // Forcer la mort de tous les monstres restants
                foreach (var monster in _mapMonsters.Where(m => !m.IsDead))
                {
                    monster.IsDead = true;
                }

                // Déclencher OnMapClean sur la map
                _currentTarget = _currentMap;
                _currentEventName = "OnMapClean";
            }
        }

        private void HandleEffPacket(EffPacket packet)
        {
            // Logique du vieux tool pour détecter IsTarget et IsBonus
            if (packet.Type == 3)
            {
                var monster = _allMonsters.FirstOrDefault(m => m.EntityId == packet.EntityId);
                if (monster != null)
                {
                    if (packet.EffectId == 824 && !monster.IsTarget)
                    {
                        monster.IsTarget = true;
                    }
                    if (packet.EffectId == 826 && !monster.IsBonus)
                    {
                        monster.IsBonus = true;
                    }
                }
            }
        }

        private void HandleSendPacket(string packet)
        {
            if (_flag1)
            {
                _currentMap.SendPackets.Add(packet);
            }
            else
            {
                AddEventToCurrentTarget("SendPacket", packet);
            }
        }

        private void AddEventToCurrentTarget(string eventType, object data)
        {
            var evt = new TimeSpaceEvent { Type = eventType, Data = data };

            if (_currentTarget is TimeSpaceMap map)
            {
                if (_currentEventName == "OnCharacterDiscoveringMap")
                {
                    map.OnCharacterDiscoveringMap.Add(evt);
                }
                else if (_currentEventName == "OnMoveOnMap")
                {
                    map.OnMoveOnMap.Add(evt);
                }
                else if (_currentEventName == "OnMapClear")
                {
                    map.OnMapClean.Add(evt);
                }
            }
            else if (_currentTarget is TimeSpaceMonster monster)
            {
                monster.OnDeathEvents.Add(evt);
            }
            else if (_currentTarget is TimeSpaceButton button)
            {
                button.OnFirstEnable.Add(evt);
            }
            else if (_currentTarget is TimeSpacePortal portal)
            {
                portal.OnTraversalEvents.Add(evt);
            }
        }

        private void GenerateXmlStructure()
        {
            var createMaps = new List<CreateMap>();

            foreach (var mapEntry in _maps.OrderBy(m => m.Key))
            {
                var map = mapEntry.Value;
                var createMap = new CreateMap
                {
                    Map = map.Id,
                    VNum = map.VNum,
                    IndexX = (byte)map.IndexX,
                    IndexY = (byte)map.IndexY
                };

                // Ordre exact de Untitled-1.xml : OnCharacterDiscoveringMap, OnMoveOnMap, SpawnButton, SpawnPortal
                GenerateOnCharacterDiscoveringMap(createMap, map);
                GenerateOnMoveOnMap(createMap, map);
                GenerateSpawnButtons(createMap, map);
                GenerateSpawnPortals(createMap, map);

                createMaps.Add(createMap);
            }

            _model.InstanceEvents.CreateMap = createMaps.ToArray();
        }

        private void GenerateOnCharacterDiscoveringMap(CreateMap createMap, TimeSpaceMap map)
        {
            if (map.NpcDialogs.Any() || map.SendMessages.Any() || map.SendPackets.Any() ||
                map.SummonNpcs.Any() || (map.Id == 0 && map.SpawnPortals.Any()))
            {
                var discovering = new OnCharacterDiscoveringMap();

                // Ordre exact de Untitled-1.xml
                discovering.NpcDialog = map.NpcDialogs.Select(d => new NpcDialog { Value = d }).ToArray();
                discovering.SendMessage = map.SendMessages.Select(m => new SendMessage { Value = m.Value, Type = m.Type }).ToArray();
                discovering.SendPacket = map.SendPackets.Select(p => new SendPacket { Value = p }).ToArray();
                discovering.SummonNpc = map.SummonNpcs.Select(n => new SummonNpc
                {
                    VNum = n.VNum,
                    PositionX = n.PositionX,
                    PositionY = n.PositionY,
                    Move = n.Move,
                    IsProtected = n.IsProtected
                }).ToArray();

                // SpawnPortal dans OnCharacterDiscoveringMap SEULEMENT pour Map 0
                if (map.Id == 0)
                {
                    discovering.SpawnPortal = map.SpawnPortals.Take(1).Select(p => new SpawnPortal
                    {
                        IdOnMap = p.IdOnMap,
                        PositionX = p.PositionX,
                        PositionY = p.PositionY,
                        Type = p.Type,
                        ToMap = p.ToMap,
                        ToX = p.ToX,
                        ToY = p.ToY
                    }).ToArray();
                }

                createMap.OnCharacterDiscoveringMap = discovering;
            }
        }

        private void GenerateOnMoveOnMap(CreateMap createMap, TimeSpaceMap map)
        {
            // OnMoveOnMap existe toujours, même si vide
            var moveOnMapList = new List<OnMoveOnMap>();
            var moveOnMap = new OnMoveOnMap();

            // Ajouter les monstres avec OnDeath
            if (map.SummonMonsters.Any())
            {
                moveOnMap.SummonMonster = map.SummonMonsters.Select(m => new SummonMonster
                {
                    VNum = m.VNum,
                    PositionX = m.PositionX,
                    PositionY = m.PositionY,
                    Move = m.Move,
                    IsBonus = m.IsBonus,
                    IsHostile = m.IsHostile,
                    OnDeath = GenerateOnDeath(m)
                }).ToArray();
            }

            if (map.GenerateClock > 0)
            {
                moveOnMap.GenerateClock = new GenerateClock { Value = map.GenerateClock };
            }

            if (map.HasStartClock)
            {
                moveOnMap.StartClock = new StartClock();
            }

            // Générer OnMapClean dans OnMoveOnMap si des événements existent
            if (map.OnMapClean.Any())
            {
                var onMapClean = new OnMapClean();

                var changePortalTypes = new List<ChangePortalType>();
                var sendMessages = new List<SendMessage>();
                var npcDialogs = new List<NpcDialog>();
                bool hasRefreshMapItems = false;

                foreach (var evt in map.OnMapClean)
                {
                    switch (evt.Type)
                    {
                        case "ChangePortalType":
                            if (evt.Data is TimeSpaceEvent changeEvt)
                            {
                                changePortalTypes.Add(new ChangePortalType
                                {
                                    IdOnMap = changeEvt.PortalId,
                                    Type = (sbyte)changeEvt.Value
                                });
                            }
                            break;
                        case "SendMessage":
                            if (evt.Data is TimeSpaceMessage msg)
                            {
                                sendMessages.Add(new SendMessage { Value = msg.Value, Type = msg.Type });
                            }
                            break;
                        case "NpcDialog":
                            if (evt.Data is int dialogId)
                            {
                                npcDialogs.Add(new NpcDialog { Value = dialogId });
                            }
                            break;
                        case "RefreshMapItems":
                            hasRefreshMapItems = true;
                            break;
                    }
                }

                onMapClean.ChangePortalType = changePortalTypes.ToArray();
                onMapClean.SendMessage = sendMessages.ToArray();
                onMapClean.NpcDialog = npcDialogs.ToArray();

                if (hasRefreshMapItems)
                {
                    onMapClean.RefreshMapItems = new object();
                }

                moveOnMap.OnMapClean = onMapClean;
            }

            moveOnMapList.Add(moveOnMap);
            createMap.OnMoveOnMap = moveOnMapList.ToArray();
        }

        private OnDeath GenerateOnDeath(TimeSpaceMonster monster)
        {
            if (!monster.OnDeathEvents.Any()) return null;

            var onDeath = new OnDeath();

            var summonMonsters = new List<SummonMonster>();
            var changePortalTypes = new List<ChangePortalType>();
            var sendMessages = new List<SendMessage>();
            var npcDialogs = new List<NpcDialog>();
            var sendPackets = new List<SendPacket>();
            bool hasRefreshMapItems = false;

            foreach (var evt in monster.OnDeathEvents)
            {
                switch (evt.Type)
                {
                    case "SummonMonster":
                        if (evt.Data is TimeSpaceMonster m)
                        {
                            summonMonsters.Add(new SummonMonster
                            {
                                VNum = m.VNum,
                                PositionX = m.PositionX,
                                PositionY = m.PositionY,
                                Move = m.Move,
                                IsBonus = m.IsBonus,
                                IsHostile = m.IsHostile
                            });
                        }
                        break;
                    case "ChangePortalType":
                        if (evt.Data is TimeSpaceEvent changeEvt)
                        {
                            changePortalTypes.Add(new ChangePortalType
                            {
                                IdOnMap = changeEvt.PortalId,
                                Type = (sbyte)changeEvt.Value
                            });
                        }
                        break;
                    case "SendMessage":
                        if (evt.Data is TimeSpaceMessage msg)
                        {
                            sendMessages.Add(new SendMessage { Value = msg.Value, Type = msg.Type });
                        }
                        break;
                    case "NpcDialog":
                        if (evt.Data is int dialogId)
                        {
                            npcDialogs.Add(new NpcDialog { Value = dialogId });
                        }
                        break;
                    case "RefreshMapItems":
                        hasRefreshMapItems = true;
                        break;
                }
            }

            onDeath.SummonMonster = summonMonsters.ToArray();
            onDeath.ChangePortalType = changePortalTypes.ToArray();
            onDeath.SendMessage = sendMessages.ToArray();
            onDeath.NpcDialog = npcDialogs.ToArray();
            onDeath.SendPacket = sendPackets.ToArray();

            if (hasRefreshMapItems)
            {
                onDeath.RefreshMapItems = new object();
            }

            return onDeath;
        }

        private void GenerateSpawnButtons(CreateMap createMap, TimeSpaceMap map)
        {
            if (map.SpawnButtons.Any())
            {
                createMap.SpawnButton = map.SpawnButtons.Select(b => new SpawnButton
                {
                    Id = b.Id,
                    PositionX = b.PositionX,
                    PositionY = b.PositionY,
                    VNumEnabled = b.VNumEnabled,
                    VNumDisabled = b.VNumDisabled,
                    OnFirstEnable = b.OnFirstEnable.Any() ? new OnFirstEnable() : null
                }).ToArray();
            }
        }

        private void GenerateSpawnPortals(CreateMap createMap, TimeSpaceMap map)
        {
            // SpawnPortal pour toutes les maps SAUF Map 0 (qui les a dans OnCharacterDiscoveringMap)
            if (map.SpawnPortals.Any())
            {
                var portalsToAdd = map.Id == 0 ? new List<TimeSpacePortal>() : map.SpawnPortals.ToList();

                if (portalsToAdd.Any())
                {
                    createMap.SpawnPortal = portalsToAdd.Select(p => new SpawnPortal
                    {
                        IdOnMap = p.IdOnMap,
                        PositionX = p.PositionX,
                        PositionY = p.PositionY,
                        Type = p.Type,
                        ToMap = p.ToMap,
                        ToX = p.ToX,
                        ToY = p.ToY,
                        OnTraversal = p.OnTraversalEvents.Any() ? new OnTraversal
                        {
                            End = p.OnTraversalEvents.Any(e => e.Type == "End") ? new End { Type = 5 } : null
                        } : null
                    }).ToArray();
                }
            }
        }

        // Classes helper
        private class TimeSpaceMap
        {
            public int Id { get; set; }
            public short VNum { get; set; }
            public short IndexX { get; set; }
            public short IndexY { get; set; }
            public List<TimeSpaceEvent> OnCharacterDiscoveringMap { get; set; }
            public List<TimeSpaceEvent> OnMoveOnMap { get; set; }
            public List<TimeSpaceEvent> OnMapClean { get; set; }
            public List<TimeSpacePortal> SpawnPortals { get; set; }
            public List<TimeSpaceNpc> SummonNpcs { get; set; }
            public List<TimeSpaceMonster> SummonMonsters { get; set; }
            public List<TimeSpaceButton> SpawnButtons { get; set; }
            public List<TimeSpaceMessage> SendMessages { get; set; }
            public List<string> SendPackets { get; set; }
            public List<int> NpcDialogs { get; set; }
            public int GenerateClock { get; set; }
            public int GenerateMapClock { get; set; }
            public bool HasStartClock { get; set; }
            public bool HasStartMapClock { get; set; }
            public HashSet<string> ProcessedPortals { get; set; }
            public HashSet<int> ProcessedButtons { get; set; }
        }

        private class TimeSpaceMonster
        {
            public short VNum { get; set; }
            public int EntityId { get; set; }
            public short PositionX { get; set; }
            public short PositionY { get; set; }
            public bool IsDead { get; set; }
            public bool IsBonus { get; set; }
            public bool IsTarget { get; set; }
            public bool Move { get; set; }
            public bool IsHostile { get; set; }
            public List<TimeSpaceEvent> OnDeathEvents { get; set; }
        }

        private class TimeSpaceNpc
        {
            public short VNum { get; set; }
            public int EntityId { get; set; }
            public short PositionX { get; set; }
            public short PositionY { get; set; }
            public bool Move { get; set; }
            public bool IsProtected { get; set; }
        }

        private class TimeSpacePortal
        {
            public byte IdOnMap { get; set; }
            public short PositionX { get; set; }
            public short PositionY { get; set; }
            public short Type { get; set; }
            public short ToMap { get; set; }
            public short ToX { get; set; }
            public short ToY { get; set; }
            public List<TimeSpaceEvent> OnTraversalEvents { get; set; }
        }

        private class TimeSpaceButton
        {
            public int Id { get; set; }
            public short PositionX { get; set; }
            public short PositionY { get; set; }
            public short VNumEnabled { get; set; }
            public short VNumDisabled { get; set; }
            public List<TimeSpaceEvent> OnFirstEnable { get; set; }
        }

        private class TimeSpaceMessage
        {
            public string Value { get; set; }
            public byte Type { get; set; }
        }

        private class TimeSpaceEvent
        {
            public string Type { get; set; }
            public object Data { get; set; }
            public int Value { get; set; }
            public int PortalId { get; set; }
        }
    }
}