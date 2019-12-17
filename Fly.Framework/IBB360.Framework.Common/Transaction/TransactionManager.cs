using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Transactions;

namespace Fly.Framework.Common
{
    public static class TransactionManager
    {
        private const string NODE_NAME = "SupportTransaction";

        private static bool SupportTransaction()
        {
            string s = ConfigurationManager.AppSettings[NODE_NAME];
            if (s != null && s.Trim().Length > 0)
            {
                s = s.Trim().ToUpper();
                if (s == "0" || s == "N" || s == "FALSE" || s == "NO")
                {
                    return false;
                }
            }
            return true;
        }

        public static ITransaction Create()
        {
            return SupportTransaction() ? (ITransaction)(new TransactionScopeWrapper()) : (ITransaction)(new NotSupportTransaction());
        }

        public static ITransaction Suppress()
        {
            return SupportTransaction() ? (ITransaction)(new TransactionScopeWrapper(TransactionScopeOption.Suppress)) : (ITransaction)(new NotSupportTransaction());
        }

        public static void InTransaction(Action action)
        {
            if (action == null)
            {
                return;
            }
            using (ITransaction scope = Create())
            {
                action();
                scope.Complete();
            }
        }

        public static void SuppressTransaction(Action action)
        {
            if (action == null)
            {
                return;
            }
            using (ITransaction scope = Suppress())
            {
                action();
                scope.Complete();
            }
        }

        private sealed class TransactionScopeWrapper : ITransaction
        {
            private readonly TransactionScope m_Scope;

            public TransactionScopeWrapper()
            {
                m_Scope = TransactionScopeFactory.CreateTransactionScope();
            }

            public TransactionScopeWrapper(TransactionScopeOption tso)
            {
                m_Scope = TransactionScopeFactory.CreateTransactionScope(tso);
            }

            public void Complete()
            {
                m_Scope.Complete();
            }

            public void Dispose()
            {
                m_Scope.Dispose();
            }
        }

        private sealed class NotSupportTransaction : ITransaction
        {
            public void Complete() { }

            public void Dispose() { }
        }
    }
}
