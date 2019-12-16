using Fly.Framework.Common;
using Fly.APIs.Common;
using Fly.Objects.Common;
using Fly.UI.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Fly.ShopAdmin.UI.Controllers
{
    public class StaffUserController : BaseController
    {
        

        [HttpPost]
        public ActionResult AjaxChangePassword(string oldPassword, string newPassword)
        {
            bool r = Api<IStaffUserApi>.Instance.CheckPassword(ContextManager.Current.UserId, oldPassword);
            if (r == false)
            {
                throw new BizException("原登录密码错误");
            }
            Api<IStaffUserApi>.Instance.ChangePassword(ContextManager.Current.UserId, newPassword);
            return Json();
        }

        public ActionResult AjaxSetValid(Guid userId, bool valid)
        {
            if (valid)
            {
                Api<IStaffUserApi>.Instance.Enable(userId);
            }
            else
            {
                Api<IStaffUserApi>.Instance.Disable(userId);
            }
            return Json();
        }

        [HttpPost]
        public JsonResult AjaxDoCreateUser(string userName, string displayName, string password, string password2, string email, string mobile, string roles)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new BizException("登录名不能为空！");

            if (string.IsNullOrWhiteSpace(displayName))
                throw new BizException("显示名不能为空！");

            if (string.IsNullOrWhiteSpace(password))
                throw new BizException("密码不能为空！");

            if (string.IsNullOrWhiteSpace(password2))
                throw new BizException("确认密码不能为空！");

            if (password != password2)
                throw new BizException("密码和确认密码不同！");

            List<Guid> roleList = Utility.ConvertStringToGuidList(roles);

            Guid uid = Guid.Empty;
            using (var tran = TransactionManager.Create())
            {
                uid = Api<IStaffUserApi>.Instance.Create(userName, displayName, password, email, mobile);
                Api<IStaffUserApi>.Instance.SetRoles(uid, roleList);
                tran.Complete();
            }

            //long? idnumber = Api<IStaffUserApi>.Instance.GetIdNumber(uid);
            string url = LinkUrl("StaffUser/UserDetail", new { id = uid });
            return Json(new { url = url });
        }

        [HttpPost]
        public JsonResult AjaxDoUpdateUser(Guid adminUserId, string displayName, string password, string password2, string email, string mobile, string roles)
        {
            StaffUser adminUser = Api<IStaffUserApi>.Instance.Get(adminUserId);
            if (adminUser == null)
                throw new BizException("ID为'{0}'的系统用户不存在！", adminUserId);

            List<Guid> roleList = Utility.ConvertStringToGuidList(roles);

            if (string.IsNullOrWhiteSpace(password) == false)
            {
                if (string.IsNullOrWhiteSpace(password2))
                    throw new BizException("确认密码不能为空！");

                if (password != password2)
                    throw new BizException("密码和确认密码不同！");
            }

            using (var tran = TransactionManager.Create())
            {
                if (string.IsNullOrWhiteSpace(displayName) == false)
                {
                    Api<IStaffUserApi>.Instance.Update(adminUserId, displayName, email, mobile);
                }

                if (string.IsNullOrWhiteSpace(password) == false)
                {
                    Api<IStaffUserApi>.Instance.ChangePassword(adminUserId, password);
                }

                Api<IStaffUserApi>.Instance.SetRoles(adminUserId, roleList);

                tran.Complete();
            }

            string url = LinkUrl("StaffUser/UserDetail", new { id = adminUserId });
            return Json(new { url = url });
        }

        private List<string> ParsePermissions(string permissions)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrWhiteSpace(permissions) == false)
            {
                string[] arr = permissions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                list = arr.Select(x => x.Trim()).ToList();
            }
            return list;
        }

        [HttpPost]
        public JsonResult AjaxDoCreateRole(string roleName, string roleDesc, string users, string permissions)
        {
            List<Guid> staffUserIds = Utility.ConvertStringToGuidList(users);
            List<string> permissionList = ParsePermissions(permissions);

            Guid roleId;
            using (var tran = TransactionManager.Create())
            {
                roleId = Api<IStaffRoleApi>.Instance.Create(roleName, roleDesc);
                Api<IStaffRoleApi>.Instance.SetPermissionKeys(roleId, permissionList);
                Api<IStaffRoleApi>.Instance.SetUsers(roleId, staffUserIds);
                tran.Complete();
            }

            string url = LinkUrl("StaffUser/RoleDetail", new { id = roleId });
            return Json(new { url = url });
        }

        [HttpPost]
        public JsonResult AjaxDoUpdateRole(Guid? roleId, string roleName, string roleDesc, string users, string permissions)
        {
            if (roleId.HasValue == false)
                throw new ApplicationException("roleId不能为空！");

            List<Guid> staffUserIds = Utility.ConvertStringToGuidList(users);
            List<string> permissionList = this.ParsePermissions(permissions);

            using (var tran = TransactionManager.Create())
            {
                Api<IStaffRoleApi>.Instance.Update(roleId.Value, roleName, roleDesc);
                Api<IStaffRoleApi>.Instance.SetPermissionKeys(roleId.Value, permissionList);
                Api<IStaffRoleApi>.Instance.SetUsers(roleId.Value, staffUserIds);
                tran.Complete();
            }

            string url = LinkUrl("StaffUser/RoleDetail", new { id = roleId });
            return Json(new { url = url });
        }

        [HttpPost]
        public ActionResult AjaxDoRemoveTempPermission(Guid? userId, string key)
        {
            if (userId.HasValue == false)
                throw new ApplicationException("用户Id错误！");

            if (string.IsNullOrWhiteSpace(key))
                throw new BizException("权限不能为空！");

            Api<IStaffUserApi>.Instance.RemoveTempPermissionSetting(userId.Value, key);

            return Json();
        }

        [HttpPost]
        public JsonResult AjaxDoSetTempPermission(Guid? userId, string json)
        {
            if (userId.HasValue == false)
            {
                throw new ApplicationException("userId不能为空");
            }

            IList<PermissionKey> allPermissions = Api<IStaffPermissionKeyApi>.Instance.GetAllPermissionKeys();
            List<PermissionKey> allSubPermissions = allPermissions.SelectMany(x => x.PermissionKeys).ToList();

            List<Tuple<string, DateTime, DateTime>> perlist = new List<Tuple<string, DateTime, DateTime>>();
            if (string.IsNullOrWhiteSpace(json) == false)
            {
                var list = DynamicJson.Parse(json);
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        string textStr = item.Text as string;
                        string valueStr = item.Value as string;
                        string setTypeStr = item.SetType as string;
                        //string maxTimesStr = item.MaxTimes as string;

                        if (string.IsNullOrWhiteSpace(valueStr))
                            continue;

                        if (allSubPermissions.Exists(x => string.Equals(x.Value, valueStr, StringComparison.OrdinalIgnoreCase)) == false)
                            throw new BizException("Key为'{0}'的权限点不存在！", valueStr);

                        if (perlist.Exists(x => string.Equals(x.Item1, valueStr, StringComparison.OrdinalIgnoreCase)))
                            throw new BizException("同一操作中，不能重复添加名为'{0}'的权限点！", textStr);

                        DateTime from, to;
                        if (string.Equals(setTypeStr, "FastSet", StringComparison.OrdinalIgnoreCase))
                        {
                            string dayStr = item.Day as string;
                            string hourStr = item.Hour as string;
                            string minuteStr = item.Minute as string;

                            if (string.IsNullOrWhiteSpace(dayStr) && string.IsNullOrWhiteSpace(hourStr) && string.IsNullOrWhiteSpace(minuteStr))
                                throw new BizException("快速设置的'天'、'时'、'分'不能都为空！");

                            int dayVal = 0;
                            if (string.IsNullOrWhiteSpace(dayStr) == false)
                            {
                                if (int.TryParse(dayStr, out dayVal) == false || dayVal < 0)
                                    throw new BizException("快速设置的'天'错误！");
                            }

                            int hourVal = 0;
                            if (string.IsNullOrWhiteSpace(hourStr) == false)
                            {
                                if (int.TryParse(hourStr, out hourVal) == false || hourVal < 0)
                                    throw new BizException("快速设置的'时'错误！");
                            }

                            int minuteVal = 0;
                            if (string.IsNullOrWhiteSpace(minuteStr) == false)
                            {
                                if (int.TryParse(minuteStr, out minuteVal) == false || minuteVal < 0)
                                    throw new BizException("快速设置的'分'错误！");
                            }

                            if (dayVal == 0 && hourVal == 0 && minuteVal == 0)
                                throw new BizException("快速设置的'天'、'时'、'分'不能都为0！");

                            from = DateTime.Now;
                            to = DateTime.Now.AddDays(dayVal).AddHours(hourVal).AddMinutes(minuteVal);
                        }
                        else if (string.Equals(setTypeStr, "ManualSet", StringComparison.OrdinalIgnoreCase))
                        {
                            string fromStr = item.From as string;
                            string toStr = item.To as string;

                            if (string.IsNullOrWhiteSpace(fromStr))
                                throw new BizException("手动选择的开始时间不能为空！");
                            if (DateTime.TryParse(fromStr, out from) == false)
                                throw new BizException("手动选择的开始时间错误！");

                            if (string.IsNullOrWhiteSpace(toStr))
                                throw new BizException("手动选择的结束时间不能为空！");
                            if (DateTime.TryParse(toStr, out to) == false)
                                throw new BizException("手动选择的结束时间错误！");
                        }
                        else
                        {
                            throw new BizException("请选择'快速设置'或者'手动选择'！");
                        }

                        //int? maxTimesVal = null;
                        //if (string.IsNullOrWhiteSpace(maxTimesStr) == false)
                        //{
                        //    int tmp;
                        //    if (int.TryParse(maxTimesStr, out tmp) == false || tmp <= 0)
                        //        throw new BizException("最大使用次数错误！");
                        //    maxTimesVal = tmp;
                        //}

                        perlist.Add(new Tuple<string, DateTime, DateTime>(valueStr, from, to));
                    }
                }
            }

            if (perlist == null || perlist.Count <= 0)
                throw new BizException("请先添加权限！");

            using (var tran = TransactionManager.Create())
            {
                foreach (var tuple in perlist)
                {
                    Api<IStaffUserApi>.Instance.AddTempPermissionSetting(userId.Value, tuple.Item1, tuple.Item2, tuple.Item3); //, tuple.Item4);
                }
                tran.Complete();
            }

            string url = LinkUrl("StaffUser/UserDetail", new { id = userId });
            return Json(new { url = url });
        }
    }
}