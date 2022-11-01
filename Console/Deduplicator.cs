namespace LoraRouter
{
    internal class Deduplicator
    {
        private const int FLUSH_INTERVAL_MS = 30000;
        private const int HOLD_TIME_MS = 10000;

        private Dictionary<string, Dictionary<string, DateTime>> _messageHistory = new Dictionary<string, Dictionary<string, DateTime>>();
        private TimeSpan _dedupDelay = new TimeSpan(0, 0, HOLD_TIME_MS/1000);
        private System.Timers.Timer _flushTimer;

        internal Deduplicator()
        {
            _flushTimer = new System.Timers.Timer(FLUSH_INTERVAL_MS);
            _flushTimer.Elapsed += _flushTimer_Elapsed;
            _flushTimer.Start();

        }

        internal bool IsDuplicate(string? data, string gatewayId)
        {
            if (!_messageHistory.ContainsKey(gatewayId))
            {
                _messageHistory.Add(gatewayId, new Dictionary<string, DateTime>());
            }

            if (_messageHistory[gatewayId].ContainsKey(data))
            {
                TimeSpan secondsAgo = DateTime.Now - _messageHistory[gatewayId][data];

                if (secondsAgo < _dedupDelay)
                {
                    return true;
                }
            }
            else
            {
                _messageHistory[gatewayId].Add(data, DateTime.Now);
            }

            return false;
        }

        private void _flushTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (string gatewayId in _messageHistory.Keys)
            {
                List<string> keys = new List<string>(_messageHistory[gatewayId].Keys);

                foreach (string key in keys)
                {
                    TimeSpan secondsAgo = DateTime.Now - _messageHistory[gatewayId][key];

                    if (secondsAgo > _dedupDelay)
                    {
                        try
                        {
                            _messageHistory[gatewayId].Remove(key);
                        }
                        catch { }
                    }
                }
            }
        }
    }
}