using System;
using System.Threading;

namespace Rook.Framework.Core
{
    public sealed class DataWaitHandle : EventWaitHandle
    {
        public DataWaitHandle(bool initialState, EventResetMode mode,
            Func<string, bool> solutionMatchFunction) : base(initialState, mode)
        {
            SolutionMatchFunction = solutionMatchFunction ?? (s => true);
        }

        public string Solution { get; private set; }
        public string Errors { get; private set; }
        
        public Func<string, bool> SolutionMatchFunction { get; }

        public bool Set(string solution, string errors)
        {
            if (errors == "[]" && !SolutionMatchFunction(solution)) return false;

            Solution = solution;
            Errors = errors;

            try
            {
                Set();
            }
            catch (ObjectDisposedException)
            {
                //EventWaitHandle has already timed out so cannot Set() it
                // I think this should return false, but it was commented out...
            }
            return true;
        }
    }
}
