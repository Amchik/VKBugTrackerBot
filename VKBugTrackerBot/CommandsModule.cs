using System;

namespace VKBugTrackerBot
{
    public abstract class CommandsModule<TContext>
    {
        protected TContext Context { get; }

        public CommandsModule(TContext context)
        {
            Context = context;
        }
    }
}
