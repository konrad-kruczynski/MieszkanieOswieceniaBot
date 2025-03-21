﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;

namespace MieszkanieOswieceniaBot.Handlers
{
    public sealed class HeartbeatenHandler : IHandler
    {
        public HeartbeatenHandler(TimeSpan timeout, params int[] relayIds)
        {
            this.relayIds = relayIds;
            this.timeout = timeout;
            turnOnActions = new Dictionary<int, Func<Task>>();
            lastHeartbeat = new DateTime(2000, 1, 1).ToUniversalTime();
        }

        public TimeSpan ProlongedTimeLeft => lastHeartbeat - DateTimeOffset.UtcNow;

        public bool CurrentState => DateTimeOffset.UtcNow - lastHeartbeat < timeout;

        public void SetActivationAction(int relayId, Func<Task> action)
        {
            // TODO: allow this only if cacheable
            turnOnActions[relayId] = action;
        }

        public async Task RefreshAsync()
        {
            var actions = relayIds.Select(ActionForRelay);
            await Task.WhenAll(actions);
        }

        public IEntry<Relays.IRelay>[] RelayEntries => relayIds.Select(x => Globals.Relays[x]).ToArray();

        public Task HeartbeatAsync()
        {
            lastHeartbeat = DateTimeOffset.UtcNow;
            return RefreshAsync();
        }

        public Task ProlongFor(TimeSpan amount)
        {
            lastHeartbeat = (lastHeartbeat < DateTimeOffset.UtcNow ? DateTimeOffset.UtcNow : lastHeartbeat) + amount;
            return RefreshAsync();
        }

        public Task ProlongAtLeastTo(DateTimeOffset value)
        {
            if (value > lastHeartbeat)
            {
                lastHeartbeat = value;
            }
            return RefreshAsync();
        }

        public string GetFriendlyTimeOffValue()
        {
            var prolongedTimeLeft = ProlongedTimeLeft;
            if (prolongedTimeLeft <= TimeSpan.Zero)
            {
                return "Przyjęto.";
            }

            string friendlyName;
            var relayEntries = RelayEntries;
            if (relayEntries.Length == 1)
            {
                friendlyName = relayEntries[0].FriendlyName;
            }
            else
            {
                friendlyName = relayEntries.Select(x => x.FriendlyName).Aggregate((x, y) => x + ", " + y);
            }

            return $"{friendlyName}: wyłączenie za {prolongedTimeLeft.Humanize(culture: Globals.BotCommunicationCultureInfo)}.";
        }

        private async Task ActionForRelay(int relayId)
        {
            if (turnOnActions.TryGetValue(relayId, out var action))
            {
                var oldState = await Globals.Relays[relayId].Element.TryGetStateAsync();
                if (oldState.Success && oldState.State != CurrentState && CurrentState)
                {
                    await action();
                }
            }
                
            await Globals.Relays[relayId].Element.TrySetStateAsync(CurrentState);
        }

        private DateTimeOffset lastHeartbeat;
        private readonly Dictionary<int, Func<Task>> turnOnActions;
        private readonly int[] relayIds;
        private readonly TimeSpan timeout;
    }
}

