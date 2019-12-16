using IBB360.Framework.Common;
using IBB360.Framework.SqlDbAccess;
using IBB360.WMSLocal.APIs.Inventory;
using IBB360.WMSLocal.APIs.Sales;
using IBB360.WMSLocal.Objects.Common;
using IBB360.WMSLocal.Objects.Sales;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Text
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var des = Country.Get("213213");

            var ss = "20191129120000".Insert(4, "-").Insert(7, "-").Insert(10, " ").Insert(13, ":").Insert(15, ":");

            var xiao = new Student()
            {

                Name = "xiao",
                Age = 12,
                MyBook = new Book()
                {
                    Name = "语文"
                }
            };

            bool sss = xiao.MyBook.ClassRoom;
            this.button1.Text = sss.ToString();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            //var id = new Guid("D54BC092-ED50-47F0-A057-2E92D89FA6C4");
            //var des = Api<ILocationInventoryApi>.Instance.GetLogsByRelatedId(id

            var sss = DateTime.Now.ToString();


        }

        private void Button3_Click(object sender, EventArgs e)
        {

            //SOPackageItem packageitem = new SOPackageItem()
            //{
            //    ItemId = new Guid("57509A39-3501-46B4-8803-880842A8969B"),
            //    Qty = 10
            //};

            //SOPackageItem packageitem1 = new SOPackageItem()
            //{
            //    ItemId = new Guid("14934B95-FF6F-48A0-8913-4037355C743A"),
            //    Qty = 3
            //};

            //SOPackage package = new SOPackage() {
            //    Weight =400,
            //    Items= new List<SOPackageItem>() { packageitem , packageitem1 }
            //};



            //Api<ISOApi>.Instance.SOOutStock(new Guid("41A21FEA-BF79-4F7D-AF99-A77E7AB2F58F"), new SOPackage[] { package });


            string sql = $@"
            INSERT INTO 
        [Text].[dbo].[text]
(
    [Id]
    ,[Remark]
)
VALUES
    (@Id,@Remark)";
            SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql, new Text() { Id=Guid.NewGuid(),Remark=null});

        }
    }
    public class Text
    {
        public Guid Id { get; set; }
        public string Remark { get; set; }
    }
    
    public class Student
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public Book MyBook { get; set; }
    }

    public class Book
    {
        public string Name { get; set; }

        public bool ClassRoom { get; set; }
    }

}
