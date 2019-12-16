using System;
using System.Collections.Generic;

namespace Fly.Framework.Common
{
    public interface ILogEmitter
    {
        void Init(Dictionary<string, string> param);

        void EmitLog(LogEntry log);
    }
}
