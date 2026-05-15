using System;
using System.Collections.Generic;
using PanzerGeneral;

namespace PanzerGeneral.Game
{
    public class TurnManager
    {
        private readonly List<int> _playerOrder;
        public int CurrentPlayerId { get; private set; }
        public int TurnNumber { get; private set; } = 1;
        public int CurrentPlayerTurnCount { get; private set; } = 0; // Licznik tur dla bieżącego gracza

        public bool IsDeployPhase { get; private set; } = true; // true = deployment, false = action
        public const int MaxUnitsPerPlayer = 5;

        private readonly Dictionary<int, int> _unitCounts = new();

        public TurnManager(IEnumerable<int> playerIds)
        {
            _playerOrder = new List<int>(playerIds);
            CurrentPlayerId = _playerOrder.Count > 0 ? _playerOrder[0] : 1;

            foreach (var playerId in _playerOrder)
            {
                _unitCounts[playerId] = 0;
            }
        }

        public void NextTurn()
        {
            int idx = _playerOrder.IndexOf(CurrentPlayerId);
            idx = (idx + 1) % _playerOrder.Count;
            CurrentPlayerId = _playerOrder[idx];

            // Jeśli wrócimy do gracza 1, zwiększ numer tury
            if (CurrentPlayerId == _playerOrder[0])
            {
                TurnNumber++;
            }

            // Przejście z deployment na action phase po turze 1
            if (TurnNumber > 1 && IsDeployPhase)
            {
                IsDeployPhase = false;
            }
        }

        public void StartTurn(IEnumerable<Unit> units)
        {
            // Reset ruchów jednostek należących do CurrentPlayerId
            foreach (var u in units)
            {
                if (u.OwnerId == CurrentPlayerId)
                {
                    u.CurrentMovePoints = u.MaxMovePoints;
                    u.HasMoved = false;
                }
            }
        }

        public bool CanDeployUnit(int playerId)
        {
            // Deployment tylko w turze 1, i tylko 5 jednostek na gracza
            if (!IsDeployPhase || TurnNumber > 1)
                return false;

            return _unitCounts.ContainsKey(playerId) && _unitCounts[playerId] < MaxUnitsPerPlayer;
        }

        public void RegisterDeployedUnit(int playerId)
        {
            if (_unitCounts.ContainsKey(playerId))
            {
                _unitCounts[playerId]++;
            }
        }

        public int GetUnitCount(int playerId)
        {
            return _unitCounts.ContainsKey(playerId) ? _unitCounts[playerId] : 0;
        }
    }
}