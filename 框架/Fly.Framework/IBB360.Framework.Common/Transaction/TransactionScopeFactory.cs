using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace Fly.Framework.Common
{
    internal class TransactionScopeFactory
    {
        public static TransactionScope CreateTransactionScope(TransactionScopeOption scopeOpion, IsolationLevel level)
        {
            if (Transaction.Current != null)
            {
                level = Transaction.Current.IsolationLevel;
            }
            return new TransactionScope(scopeOpion, new TransactionOptions { IsolationLevel = level });
        }

        public static TransactionScope CreateTransactionScope(TransactionScopeOption scopeOpion)
        {
            return CreateTransactionScope(scopeOpion, IsolationLevel.Serializable);
        }

        public static TransactionScope CreateTransactionScope()
        {
            return CreateTransactionScope(TransactionScopeOption.Required);
        }        
    }
}
