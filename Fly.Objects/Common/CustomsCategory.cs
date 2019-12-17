using System.Collections.Generic;

namespace Fly.Objects.Common
{
	/// <summary>
	/// 海关 商品种类
	/// </summary>
	public sealed class CustomsCategory
	{
		private static List<CustomsCategory> s_List = new List<CustomsCategory>
		{
			#region Customs Category List
			
			new CustomsCategory("1", "食品、饮料"),
			new CustomsCategory("2", "酒"),
			new CustomsCategory("3", "烟草"),
			new CustomsCategory("4", "纺织品及其制成品"),
			new CustomsCategory("5", "皮革服装及配饰"),
			new CustomsCategory("6", "箱包及鞋靴"),
			new CustomsCategory("19", "计算机及其外围设备"),
			new CustomsCategory("20", "书报、刊物及其他各类印刷品"),
			new CustomsCategory("21", "教育专用的电影片、幻灯片、原版录音带、录像带"),
			new CustomsCategory("22", "文具用品及玩具"),
			new CustomsCategory("23", "邮票"),
			new CustomsCategory("24", "乐器"),
			new CustomsCategory("25", "体育用品"),
			new CustomsCategory("26", "自行车、三轮车、童车及其配件、附件"),
			new CustomsCategory("27", "其他物品"),
			new CustomsCategory("7", "表、钟及其配件、附件"),
			new CustomsCategory("8", "金、银、珠宝及其制品、艺术品、收藏品"),
			new CustomsCategory("9", "化妆品"),
			new CustomsCategory("10", "家用医疗、保健及美容器材"),
			new CustomsCategory("11", "厨卫用具及小家电"),
			new CustomsCategory("12", "家具"),
			new CustomsCategory("13", "空调及其配件、附件"),
			new CustomsCategory("14", "电冰箱及其配件、附件"),
			new CustomsCategory("15", "洗衣设备及其配件、附件"),
			new CustomsCategory("16", "电视机及其配件、附件"),
			new CustomsCategory("17", "摄影（像）设备及其配件、附件"),
			new CustomsCategory("18", "影音设备及其配件、附件")

			#endregion
		};

		public static List<CustomsCategory> All
		{
			get
			{
				return new List<CustomsCategory>(s_List);
			}
		}

		public static CustomsCategory Get(string code)
		{
			return All.Find(x => string.Compare(x.Code, code, true) == 0);
		}

		public static bool IsValidCode(string code)
		{
			return Get(code) != null;
		}

		//------- Instance Member ------------------------------------

		private CustomsCategory(string code, string name)
		{
			Code = code;
			Name = name;
		}

		/// <summary>
		/// 代码
		/// </summary>
		public string Code { get; private set; }

		/// <summary>
		/// 名称
		/// </summary>
		public string Name { get; private set; }
	}
}