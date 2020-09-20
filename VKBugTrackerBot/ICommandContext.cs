using System;

namespace VKBugTrackerBot
{
    public interface ICommandContext
    {
        UserPreferences User { get; }
        String Command { get; }
        String[] Arguments { get; }

        void Send(Object @params);
    }

    public interface ICommandContext<TMessageParams> : ICommandContext
    {
        void Send(TMessageParams @params);
    }
}
