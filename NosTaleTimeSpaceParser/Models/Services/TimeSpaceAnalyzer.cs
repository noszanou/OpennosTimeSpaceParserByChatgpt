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
        private bool _flag1;
        private bool _inOnFirstEnable;
        private int _num3;
        private TimeSpaceMap _currentMap;
        private TimeSpaceMonster _lastDeadMonster;
        private short _posX, _posY;
        private short _indexX, _indexY;
        private int _playerId = -1;
        private int _buttonIdCounter = 0;

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
            _buttonIdCounter = 0;
        }

        private void ParseGlobalsFromRbr()
        {
            string descriptionLine = null;
            for (int i = 0; i < _packets.Count; i++)
            {
                if (RbrPacketParser.CanParse(_packets[i]))
                {
                    var rbr = RbrPacketParser.Parse(_packets[i]);
                    string realName = ExtractRealNameFromRbr(rbr.Name);
                    if (i + 1 < _packets.Count)
                    {
                        var nextLine = _packets[i + 1].Trim();
                        if (!IsPacketLine(nextLine) && !string.IsNullOrEmpty(nextLine))
                            descriptionLine = nextLine;
                    }
                    _model.Globals.Name = new Name { Value = realName };
                    _model.Globals.Label = new Label { Value = descriptionLine ?? rbr.Label };
                    _model.Globals.LevelMinimum = new Level { Value = (byte)rbr.LevelMinimum };
                    _model.Globals.LevelMaximum = new Level { Value = (byte)rbr.LevelMaximum };
                    _model.Globals.Lives = new Lives { Value = 1 };
                    _model.Globals.DrawItems = rbr.DrawItems.Select(item => new Item { VNum = (short)item.VNum, Amount = (short)item.Amount }).ToArray();
                    _model.Globals.SpecialItems = rbr.SpecialItems.Select(item => new Item { VNum = (short)item.VNum, Amount = (short)item.Amount }).ToArray();
                    _model.Globals.GiftItems = rbr.GiftItems.Select(item => new Item { VNum = (short)item.VNum, Amount = (short)item.Amount }).ToArray();
                    _model.Globals.Gold = new Gold { Value = 1500 };
                    _model.Globals.Reputation = new Reputation { Value = 50 };
                    break;
                }
            }
        }

        private string ExtractRealNameFromRbr(string fullName)
        {
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
                return string.Join(" ", parts.Skip(nameStartIndex));
            return fullName;
        }

        private bool IsNumericOrScore(string part)
        {
            return part == "0" || part == "0." || int.TryParse(part, out _) || float.TryParse(part, out _) || part.Contains(".");
        }

        private bool IsPacketLine(string line)
        {
            var packetPrefixes = new[] { "at ", "in ", "gp ", "msg ", "walk ", "su ", "eff ", "evnt ", "npc_req ", "out ", "preq", "rsfn ", "rsfm ", "rsfp " };
            return packetPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private void ProcessPacketsSequentially()
        {
            for (int i = 0; i < _packets.Count; i++)
                ProcessSinglePacket(_packets[i]);
        }

        private void ProcessSinglePacket(string packet)
        {
            if (AtPacketParser.CanParse(packet))
                HandleAtPacket(AtPacketParser.Parse(packet));
            else if (RsfPacketParser.CanParse(packet))
                HandleRsfnPacket(RsfPacketParser.Parse(packet));
            else if (WalkPacketParser.CanParse(packet))
                HandleWalkPacket(WalkPacketParser.Parse(packet));
            else if (InPacketParser.CanParse(packet))
                HandleInPacket(InPacketParser.Parse(packet));
            else if (SuPacketParser.CanParse(packet))
                HandleSuPacket(SuPacketParser.Parse(packet));
            else if (GpPacketParser.CanParse(packet))
                HandleGpPacket(GpPacketParser.Parse(packet));
            else if (MsgPacketParser.CanParse(packet))
                HandleMsgPacket(MsgPacketParser.Parse(packet));
            else if (NpcReqPacketParser.CanParse(packet))
                HandleNpcReqPacket(NpcReqPacketParser.Parse(packet));
            else if (EvntPacketParser.CanParse(packet))
                HandleEvntPacket(EvntPacketParser.Parse(packet));
            else if (OutPacketParser.CanParse(packet))
                HandleOutPacket(OutPacketParser.Parse(packet));
            else if (PreqPacketParser.CanParse(packet))
                HandlePreqPacket();
            else if (EffPacketParser.CanParse(packet))
                HandleEffPacket(EffPacketParser.Parse(packet));
            else if (packet.Trim().StartsWith("mapclear") || packet.Trim().StartsWith("mapclean"))
                HandleMapClear();
            else if (packet.Trim().StartsWith("rsfm ") || packet.Trim().StartsWith("sinfo ") || packet.Trim().StartsWith("minfo ") || packet.Trim().StartsWith("msgi "))
                HandleSendPacket(packet.Trim());
        }

        private void HandleAtPacket(AtPacket packet)
        {
            if (_playerId == -1)
                _playerId = packet.MapId;

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
                OnFirstEnableEvents = new List<TimeSpaceEvent>(),
                SpawnPortals = new List<TimeSpacePortal>(),
                SummonNpcs = new List<TimeSpaceNpc>(),
                SummonMonsters = new List<TimeSpaceMonster>(),
                SpawnButtons = new List<TimeSpaceButton>(),
                SendMessages = new List<TimeSpaceMessage>(),
                SendPackets = new List<string>(),
                NpcDialogs = new List<int>(),
                ProcessedPortals = new HashSet<string>(),
                ProcessedButtons = new HashSet<int>()
            };

            _maps[mapId] = _currentMap;
            _mapMonsters.Clear();
            _flag1 = true;
            _inOnFirstEnable = false;
            _num3 = 0;
            _posX = (short)packet.PositionX;
            _posY = (short)packet.PositionY;
        }

        private void HandleRsfnPacket(RsfPacket packet)
        {
            if (packet.Values.Count >= 2)
            {
                _indexX = (short)packet.Values[0];
                _indexY = (short)packet.Values[1];
                if (_currentMap != null)
                {
                    _currentMap.IndexX = _indexX;
                    _currentMap.IndexY = _indexY;
                }
            }
        }

        private void HandleWalkPacket(WalkPacket packet)
        {
            _posX = (short)packet.PositionX;
            _posY = (short)packet.PositionY;
            _num3++;
        }

        private void HandleInPacket(InPacket packet)
        {
            if (packet.RawPacket.Contains(" @ "))
                return;

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

        private void HandleNpcSummon(InPacket packet)
        {
            var npc = new TimeSpaceNpc
            {
                VNum = (short)packet.VNum,
                EntityId = packet.EntityId,
                PositionX = (short)packet.PositionX,
                PositionY = (short)packet.PositionY,
                Move = true,
                IsProtected = packet.VNum == 320
            };

            if (_flag1)
                _currentMap.SummonNpcs.Add(npc);
            else if (_inOnFirstEnable)
                _currentMap.OnFirstEnableEvents.Add(new TimeSpaceEvent { Type = "SummonNpc", Data = npc });
            else
                _currentMap.OnMoveOnMap.Add(new TimeSpaceEvent { Type = "SummonNpc", Data = npc });
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
                IsBonus = packet.VNum == 24,
                IsTarget = false,
                Move = true,
                IsHostile = packet.VNum == 333,
                OnDeathEvents = new List<TimeSpaceEvent>()
            };

            _allMonsters.Add(monster);

            if (_flag1)
            {
                _currentMap.SummonMonsters.Add(monster);
                _mapMonsters.Add(monster);
            }
            else if (_inOnFirstEnable)
            {
                _currentMap.OnFirstEnableEvents.Add(new TimeSpaceEvent { Type = "SummonMonster", Data = monster });
            }
            else if (_lastDeadMonster != null)
            {
                _lastDeadMonster.OnDeathEvents.Add(new TimeSpaceEvent { Type = "SummonMonster", Data = monster });
            }
            else
            {
                _currentMap.OnMoveOnMap.Add(new TimeSpaceEvent { Type = "SummonMonster", Data = monster });
                _mapMonsters.Add(monster);
            }
        }

        private void HandleObjectSummon(InPacket packet)
        {
            if (_currentMap.ProcessedButtons.Contains(packet.EntityId))
                return;

            _currentMap.ProcessedButtons.Add(packet.EntityId);

            short enableVNum, disableVNum;
            switch (packet.VNum)
            {
                case 1000:
                    enableVNum = 1045;
                    disableVNum = 1000;
                    break;
                case 1057:
                    enableVNum = 1057;
                    disableVNum = 1057;
                    break;
                default:
                    enableVNum = (short)(packet.VNum + 1);
                    disableVNum = (short)packet.VNum;
                    break;
            }

            var button = new TimeSpaceButton
            {
                Id = _buttonIdCounter++,
                PositionX = (short)packet.PositionX,
                PositionY = (short)packet.PositionY,
                VNumEnabled = enableVNum,
                VNumDisabled = disableVNum,
                OnFirstEnable = new List<TimeSpaceEvent>()
            };

            _currentMap.SpawnButtons.Add(button);
        }

        private void HandleSuPacket(SuPacket packet)
        {
            if (packet.Type == 1 && packet.CallerId == _playerId)
            {
                var monster = _allMonsters.FirstOrDefault(m => m.EntityId == packet.TargetId);
                if (monster != null && !monster.IsDead)
                {
                    monster.IsDead = true;
                    _lastDeadMonster = monster;
                }
            }
        }

        private void HandleMapClear()
        {
            foreach (var monster in _mapMonsters.Where(m => !m.IsDead))
                monster.IsDead = true;

            if (_flag1)
                _flag1 = false;
            else if (_inOnFirstEnable)
                _currentMap.OnFirstEnableEvents.Add(new TimeSpaceEvent { Type = "OnMapClean", Data = null });
            else
                _currentMap.OnMapClean.Add(new TimeSpaceEvent { Type = "OnMapClean", Data = null });

            _lastDeadMonster = null;
        }

        private void HandleGpPacket(GpPacket packet)
        {
            var portalKey = $"{packet.PortalId}_{packet.SourceX}_{packet.SourceY}_{packet.Type}";
            if (_currentMap.ProcessedPortals.Contains(portalKey))
                return;

            _currentMap.ProcessedPortals.Add(portalKey);

            if (packet.Type >= 2)
            {
                var changeEvent = new TimeSpaceEvent { Type = "ChangePortalType", PortalId = packet.PortalId, Value = packet.Type };
                if (_inOnFirstEnable)
                {
                    _currentMap.OnFirstEnableEvents.Add(changeEvent);
                    _currentMap.OnFirstEnableEvents.Add(new TimeSpaceEvent { Type = "RefreshMapItems" });
                }
                else
                {
                    _currentMap.OnMapClean.Add(changeEvent);
                    _currentMap.OnMapClean.Add(new TimeSpaceEvent { Type = "RefreshMapItems" });
                }
                return;
            }

            short toMap;
            short toX = (short)packet.SourceX;
            short toY = (short)(packet.SourceY == 1 ? 28 : 1);

            if (packet.Type == 5)
                toMap = -1;
            else
            {
                var currentMapIndex = _maps.Count - 1;
                if (packet.SourceY == 1)
                    toMap = (short)(currentMapIndex + 1);
                else if (packet.SourceY == 28)
                    toMap = (short)(currentMapIndex - 1);
                else
                    toMap = (short)currentMapIndex;
            }

            var portal = new TimeSpacePortal
            {
                IdOnMap = (byte)packet.PortalId,
                PositionX = (short)packet.SourceX,
                PositionY = (short)packet.SourceY,
                Type = (short)packet.Type,
                ToMap = toMap,
                ToX = toX,
                ToY = toY,
                OnTraversalEvents = new List<TimeSpaceEvent>()
            };

            if (packet.Type == 5)
                portal.OnTraversalEvents.Add(new TimeSpaceEvent { Type = "End", Value = 5 });

            _currentMap.SpawnPortals.Add(portal);
        }

        private void HandleMsgPacket(MsgPacket packet)
        {
            var message = new TimeSpaceMessage { Value = packet.Message, Type = (byte)packet.Type };

            if (_flag1)
                _currentMap.SendMessages.Add(message);
            else if (_inOnFirstEnable)
                _currentMap.OnFirstEnableEvents.Add(new TimeSpaceEvent { Type = "SendMessage", Data = message });
            else
                _currentMap.OnMapClean.Add(new TimeSpaceEvent { Type = "SendMessage", Data = message });
        }

        private void HandleNpcReqPacket(NpcReqPacket packet)
        {
            if (_flag1)
                _currentMap.NpcDialogs.Add(packet.DialogId);
            else if (_inOnFirstEnable)
                _currentMap.OnFirstEnableEvents.Add(new TimeSpaceEvent { Type = "NpcDialog", Data = packet.DialogId });
            else
                _currentMap.OnMapClean.Add(new TimeSpaceEvent { Type = "NpcDialog", Data = packet.DialogId });
        }

        private void HandleEvntPacket(EvntPacket packet)
        {
            switch (packet.Type)
            {
                case 1:
                    if (packet.Time1 == packet.Time2)
                    {
                        if (_flag1)
                        {
                            _currentMap.GenerateClock = packet.Time1;
                            _currentMap.HasStartClock = true;
                        }
                        else
                        {
                            _currentMap.OnMoveOnMap.Add(new TimeSpaceEvent { Type = "GenerateClock", Value = packet.Time1 });
                        }
                    }
                    break;
                case 3:
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
            if (packet.Type == 9)
                _inOnFirstEnable = true;
        }

        private void HandlePreqPacket()
        {
            _flag1 = false;
            _inOnFirstEnable = false;
            _lastDeadMonster = null;
        }

        private void HandleEffPacket(EffPacket packet)
        {
            if (packet.Type == 3)
            {
                var monster = _allMonsters.FirstOrDefault(m => m.EntityId == packet.EntityId);
                if (monster != null)
                {
                    if (packet.EffectId == 824)
                        monster.IsTarget = true;
                    if (packet.EffectId == 826)
                        monster.IsBonus = true;
                }
            }
        }

        private void HandleSendPacket(string packet)
        {
            if (_flag1)
                _currentMap.SendPackets.Add(packet);
            else if (_inOnFirstEnable)
                _currentMap.OnFirstEnableEvents.Add(new TimeSpaceEvent { Type = "SendPacket", Data = packet });
            else if (_lastDeadMonster != null)
                _lastDeadMonster.OnDeathEvents.Add(new TimeSpaceEvent { Type = "SendPacket", Data = packet });
            else
                _currentMap.OnMapClean.Add(new TimeSpaceEvent { Type = "SendPacket", Data = packet });
        }

        private void GenerateXmlStructure()
        {
            var createMaps = new List<CreateMap>();
            foreach (var mapEntry in _maps.OrderBy(m => m.Key))
            {
                var map = mapEntry.Value;
                var createMap = new CreateMap { Map = map.Id, VNum = map.VNum, IndexX = (byte)map.IndexX, IndexY = (byte)map.IndexY };
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
            if (map.NpcDialogs.Any() || map.SendMessages.Any() || map.SendPackets.Any() || map.SummonNpcs.Any() || map.SummonMonsters.Any() || (map.Id == 0 && map.SpawnPortals.Any()))
            {
                var discovering = new OnCharacterDiscoveringMap();
                discovering.NpcDialog = map.NpcDialogs.Select(d => new NpcDialog { Value = d }).ToArray();
                discovering.SendMessage = map.SendMessages.Select(m => new SendMessage { Value = m.Value, Type = m.Type }).ToArray();
                discovering.SendPacket = map.SendPackets.Select(p => new SendPacket { Value = p }).ToArray();
                discovering.SummonNpc = map.SummonNpcs.Select(n => new SummonNpc { VNum = n.VNum, PositionX = n.PositionX, PositionY = n.PositionY, Move = n.Move, IsProtected = n.IsProtected }).ToArray();
                discovering.SummonMonster = map.SummonMonsters.Select(m => new SummonMonster { VNum = m.VNum, PositionX = m.PositionX, PositionY = m.PositionY, Move = m.Move, IsBonus = m.IsBonus, IsHostile = m.IsHostile, OnDeath = GenerateOnDeath(m) }).ToArray();
                if (map.Id == 0)
                    discovering.SpawnPortal = map.SpawnPortals.Take(1).Select(p => new SpawnPortal { IdOnMap = p.IdOnMap, PositionX = p.PositionX, PositionY = p.PositionY, Type = p.Type, ToMap = p.ToMap, ToX = p.ToX, ToY = p.ToY }).ToArray();
                createMap.OnCharacterDiscoveringMap = discovering;
            }
        }

        private void GenerateOnMoveOnMap(CreateMap createMap, TimeSpaceMap map)
        {
            if (map.OnMoveOnMap.Any() || map.GenerateClock > 0 || map.OnMapClean.Any())
            {
                var moveOnMapList = new List<OnMoveOnMap>();
                var moveOnMap = new OnMoveOnMap();
                var summonMonsters = new List<SummonMonster>();
                var sendMessages = new List<SendMessage>();
                var sendPackets = new List<SendPacket>();

                foreach (var evt in map.OnMoveOnMap)
                {
                    switch (evt.Type)
                    {
                        case "SummonMonster":
                            if (evt.Data is TimeSpaceMonster m)
                                summonMonsters.Add(new SummonMonster { VNum = m.VNum, PositionX = m.PositionX, PositionY = m.PositionY, Move = m.Move, IsBonus = m.IsBonus, IsHostile = m.IsHostile, OnDeath = GenerateOnDeath(m) });
                            break;
                        case "SendMessage":
                            if (evt.Data is TimeSpaceMessage msg)
                                sendMessages.Add(new SendMessage { Value = msg.Value, Type = msg.Type });
                            break;
                        case "SendPacket":
                            if (evt.Data is string packet)
                                sendPackets.Add(new SendPacket { Value = packet });
                            break;
                        case "GenerateClock":
                            moveOnMap.GenerateClock = new GenerateClock { Value = evt.Value };
                            break;
                    }
                }

                moveOnMap.SummonMonster = summonMonsters.ToArray();
                moveOnMap.SendMessage = sendMessages.ToArray();
                moveOnMap.SendPacket = sendPackets.ToArray();
                if (map.GenerateClock > 0)
                    moveOnMap.GenerateClock = new GenerateClock { Value = map.GenerateClock };
                if (map.HasStartClock)
                    moveOnMap.StartClock = new StartClock();
                if (map.OnMapClean.Any())
                    moveOnMap.OnMapClean = GenerateOnMapClean(map.OnMapClean);
                moveOnMapList.Add(moveOnMap);
                createMap.OnMoveOnMap = moveOnMapList.ToArray();
            }
        }

        private OnMapClean GenerateOnMapClean(List<TimeSpaceEvent> events)
        {
            var onMapClean = new OnMapClean();
            var changePortalTypes = new List<ChangePortalType>();
            var sendMessages = new List<SendMessage>();
            var npcDialogs = new List<NpcDialog>();
            var sendPackets = new List<SendPacket>();
            bool hasRefreshMapItems = false;

            foreach (var evt in events)
            {
                switch (evt.Type)
                {
                    case "ChangePortalType":
                        changePortalTypes.Add(new ChangePortalType { IdOnMap = evt.PortalId, Type = (sbyte)evt.Value });
                        break;
                    case "SendMessage":
                        if (evt.Data is TimeSpaceMessage msg)
                            sendMessages.Add(new SendMessage { Value = msg.Value, Type = msg.Type });
                        break;
                    case "NpcDialog":
                        if (evt.Data is int dialogId)
                            npcDialogs.Add(new NpcDialog { Value = dialogId });
                        break;
                    case "SendPacket":
                        if (evt.Data is string packet)
                            sendPackets.Add(new SendPacket { Value = packet });
                        break;
                    case "RefreshMapItems":
                        hasRefreshMapItems = true;
                        break;
                }
            }

            onMapClean.ChangePortalType = changePortalTypes.ToArray();
            onMapClean.SendMessage = sendMessages.ToArray();
            onMapClean.NpcDialog = npcDialogs.ToArray();
            onMapClean.SendPacket = sendPackets.ToArray();
            if (hasRefreshMapItems)
                onMapClean.RefreshMapItems = new object();
            return onMapClean;
        }

        private OnDeath GenerateOnDeath(TimeSpaceMonster monster)
        {
            if (!monster.OnDeathEvents.Any())
                return null;

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
                            summonMonsters.Add(new SummonMonster { VNum = m.VNum, PositionX = m.PositionX, PositionY = m.PositionY, Move = m.Move, IsBonus = m.IsBonus, IsHostile = m.IsHostile, OnDeath = GenerateOnDeath(m) });
                        break;
                    case "ChangePortalType":
                        changePortalTypes.Add(new ChangePortalType { IdOnMap = evt.PortalId, Type = (sbyte)evt.Value });
                        break;
                    case "SendMessage":
                        if (evt.Data is TimeSpaceMessage msg)
                            sendMessages.Add(new SendMessage { Value = msg.Value, Type = msg.Type });
                        break;
                    case "NpcDialog":
                        if (evt.Data is int dialogId)
                            npcDialogs.Add(new NpcDialog { Value = dialogId });
                        break;
                    case "SendPacket":
                        if (evt.Data is string packet)
                            sendPackets.Add(new SendPacket { Value = packet });
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
                onDeath.RefreshMapItems = new object();
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
                    OnFirstEnable = GenerateOnFirstEnable(map)
                }).ToArray();
            }
        }

        private OnFirstEnable GenerateOnFirstEnable(TimeSpaceMap map)
        {
            if (!map.OnFirstEnableEvents.Any())
                return null;

            var onFirstEnable = new OnFirstEnable();
            var summonMonsters = new List<SummonMonster>();
            var sendMessages = new List<SendMessage>();
            var sendPackets = new List<SendPacket>();
            var npcDialogs = new List<NpcDialog>();
            OnMapClean onMapClean = null;

            foreach (var evt in map.OnFirstEnableEvents)
            {
                switch (evt.Type)
                {
                    case "SummonMonster":
                        if (evt.Data is TimeSpaceMonster m)
                            summonMonsters.Add(new SummonMonster { VNum = m.VNum, PositionX = m.PositionX, PositionY = m.PositionY, Move = m.Move, IsBonus = m.IsBonus, IsHostile = m.IsHostile, OnDeath = GenerateOnDeath(m) });
                        break;
                    case "SendMessage":
                        if (evt.Data is TimeSpaceMessage msg)
                            sendMessages.Add(new SendMessage { Value = msg.Value, Type = msg.Type });
                        break;
                    case "SendPacket":
                        if (evt.Data is string packet)
                            sendPackets.Add(new SendPacket { Value = packet });
                        break;
                    case "NpcDialog":
                        if (evt.Data is int dialogId)
                            npcDialogs.Add(new NpcDialog { Value = dialogId });
                        break;
                    case "OnMapClean":
                        var mapCleanEvents = map.OnFirstEnableEvents.Where(e => e.Type == "ChangePortalType" || e.Type == "RefreshMapItems" || e.Type == "SendMessage" || e.Type == "NpcDialog").ToList();
                        if (mapCleanEvents.Any())
                            onMapClean = GenerateOnMapClean(mapCleanEvents);
                        break;
                }
            }

            onFirstEnable.SummonMonster = summonMonsters.ToArray();
            onFirstEnable.SendMessage = sendMessages.ToArray();
            onFirstEnable.NpcDialog = npcDialogs.ToArray();
            onFirstEnable.OnMapClean = onMapClean;
            // IL FAUT AJOUTER SendPacket AU MODÈLE PUIS DÉCOMMENTER :
            // onFirstEnable.SendPacket = sendPackets.ToArray();
            return onFirstEnable;
        }

        private void GenerateSpawnPortals(CreateMap createMap, TimeSpaceMap map)
        {
            if (map.SpawnPortals.Any())
            {
                var portalsToAdd = map.Id == 0 ? map.SpawnPortals.Skip(1).ToList() : map.SpawnPortals.ToList();
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
                        OnTraversal = p.OnTraversalEvents.Any() ? new OnTraversal { End = p.OnTraversalEvents.Any(e => e.Type == "End") ? new End { Type = 5 } : null } : null
                    }).ToArray();
                }
            }
        }

        private class TimeSpaceMap
        {
            public int Id { get; set; }
            public short VNum { get; set; }
            public short IndexX { get; set; }
            public short IndexY { get; set; }
            public List<TimeSpaceEvent> OnCharacterDiscoveringMap { get; set; }
            public List<TimeSpaceEvent> OnMoveOnMap { get; set; }
            public List<TimeSpaceEvent> OnMapClean { get; set; }
            public List<TimeSpaceEvent> OnFirstEnableEvents { get; set; }
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