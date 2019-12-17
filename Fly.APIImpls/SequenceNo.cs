using Fly.Framework.SqlDbAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIImpls
{
    internal static class SequenceNo
    {
        public static long Generate(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException("type");
            }
            type = type.Trim();
            if (type.Length > 64)
            {
                throw new ApplicationException("种子表SequenceNo的Type字段不能超过64个字符");
            }
            string sql = @"
IF NOT EXISTS(SELECT TOP 1 1 FROM [FlyBase].[dbo].[SequenceNo] WHERE [Type]=@type)
BEGIN
	INSERT INTO [FlyBase].[dbo].[SequenceNo]
	(
		[Type], [Number]
	)
	VALUES
	(
		@type, 1
	)
	SELECT TOP 1 1 AS [Number]
END
ELSE
BEGIN
	UPDATE TOP (1) [FlyBase].[dbo].[SequenceNo] SET [Number]=[Number]+1 OUTPUT INSERTED.[Number] WHERE [Type]=@type
END";
            return SqlHelper.ExecuteScalar<long>(DbInstance.CanWrite, sql, new { type });
        }

        public static IList<long> Generate(string type, int qty)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException("type");
            }
            type = type.Trim();
            if (type.Length > 64)
            {
                throw new ApplicationException("种子表SequenceNo的Type字段不能超过64个字符");
            }
            if (qty <= 0)
            {
                throw new ApplicationException("生成种子的数量qty必须大于0");
            }
            if (qty == 1)
            {
                return new List<long> { Generate(type) };
            }
            string sql = string.Format(@"
IF NOT EXISTS(SELECT TOP 1 1 FROM [FlyBase].[dbo].[SequenceNo] WHERE [Type]=@type)
BEGIN
	INSERT INTO [FlyBase].[dbo].[SequenceNo]
	(
		[Type], [Number]
	)
	VALUES
	(
		@type, {0}
	)
	SELECT TOP 1 1 AS [From], {0} AS [To]
END
ELSE
BEGIN
	UPDATE TOP (1) [FlyBase].[dbo].[SequenceNo] SET [Number]=[Number]+{0} OUTPUT DELETED.[Number]+1 AS [From], INSERTED.[Number] AS [To] WHERE [Type]=@type
END", qty);
            long from = 0, to = 0;
            SqlHelper.ExecuteReaderFirst(r =>
            {
                from = Convert.ToInt64(r[0]);
                to = Convert.ToInt64(r[1]);
            }, DbInstance.CanWrite, sql, new { type });
            List<long> list = new List<long>((int)to - (int)from + 1);
            for (long i = from; i <= to; i++)
            {
                list.Add(i);
            }
            return list;
        }
    
    }
}