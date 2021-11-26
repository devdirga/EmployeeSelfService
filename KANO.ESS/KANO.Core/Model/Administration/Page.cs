using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace KANO.Core.Model
{
    [Collection("Pages")]
    [BsonIgnoreExtraElements]
    public class Page : BaseT, IMongoPreSave<Page>
    {
        [BsonId]
        public string Id { get; set; }
        public string PageCode { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public string ParentId { get; set; }
        public int Index { get; set; }
        public bool Enabled { get; set; }
        public bool ForWhomHasSubordinate { get; set; }
        public EmployeeShiftType ShiftType { get; set; }
        public bool ShowAsMenu { get; set; }
        public List<string> SpecialActions { get; set; } = new List<string>();
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }

        public void PreSave(IMongoDatabase db)
        {
            this.LastUpdate = DateTime.Now;
            if (string.IsNullOrEmpty(Id))
            {
                Id = Guid.NewGuid().ToString("N");
                CreateDate = DateTime.Now;
            }
            if (!string.IsNullOrEmpty(ParentId))
            {
                var visited = new List<string>();
                visited.Add(Id);
                Page parent = db.GetCollection<Page>().Find(pm => pm.Id == ParentId).FirstOrDefault();
                while (parent != null)
                {
                    if (visited.Contains(parent.Id)) throw new Exception("Circular reference");
                    visited.Add(parent.Id);
                    parent = !string.IsNullOrEmpty(parent.ParentId) ? db.GetCollection<Page>().Find(pm => pm.Id == parent.ParentId).FirstOrDefault() : null;
                }
            }
        }

        public static bool IsCodeAlreadyUsed(IMongoDatabase db, Page page)
        {
            var ret = db.GetCollection<Page>().Find(x => x.PageCode == page.PageCode && x.Id != page.Id).ToList();
            if (ret != null && ret.Count > 0)
                return false;
            else
            {
                return true;
            }
        }

        public static List<Page> Init(IMongoDatabase db, bool force = false)
        {
            var pageCount = db.GetCollection<Page>().CountDocuments(x => true);
            var pages = new List<Page>(new Page[] {
                new Page{
                    Icon="mdi mdi-home",
                    Id="Dashboard",
                    Index=0,
                    PageCode="Dashboard",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Dashboard",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Dashboard",
                },
                new Page{
                    Icon="mdi mdi-account-box",
                    Id="Employee",
                    Index=1,
                    PageCode="Employee",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Employee",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="",
                    Id="EmployeeProfile",
                    Index=0,
                    PageCode="EmployeeProfile",
                    ParentId="Employee",
                    ShowAsMenu=true,
                    Title="Profile",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Employee/Profile",
                },
                new Page{
                    Icon="",
                    Id="EmployeeCertificate",
                    Index=1,
                    PageCode="EmployeeCertificate",
                    ParentId="Employee",
                    Title="Certificate",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="",
                    Id="EmployeeFamily",
                    Index=2,
                    PageCode="EmployeeFamily",
                    ParentId="Employee",
                    Title="Family",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="",
                    Id="EmployeeEmployment",
                    Index=3,
                    PageCode="EmployeeEmployment",
                    ParentId="Employee",
                    Title="Employment",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="",
                    Id="EmployeeWarningLetter",
                    Index=4,
                    PageCode="EmployeeWarningLetter",
                    ParentId="Employee",
                    Title="Warning Letter",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="",
                    Id="EmployeeMedicalRecord",
                    Index=5,
                    PageCode="EmployeeMedicalRecord",
                    ParentId="Employee",
                    Title="Medical Record",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="",
                    Id="EmployeeDocument",
                    Index=6,
                    PageCode="EmployeeDocument",
                    ParentId="Employee",
                    Title="Document",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="",
                    Id="DocumentRequest",
                    Index=7,
                    PageCode="DocumentRequest",
                    ParentId="Employee",
                    ShowAsMenu=true,
                    Title="Document Request",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Employee/DocumentRequest",
                },
                new Page{
                    Icon="",
                    Id="EmployeeApplications",
                    Index=8,
                    PageCode="EmployeeApplications",
                    ParentId="Employee",
                    ShowAsMenu=true,
                    Title="Applications",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Employee/Applications",
                },
                new Page{
                    Icon="mdi mdi-coins",
                    Id="Payroll",
                    Index=2,
                    PageCode="Payroll",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Payroll",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="mdi mdi-coins",
                    Id="Payslip",
                    Index=2,
                    PageCode="Payslip",
                    ParentId="Payroll",
                    ShowAsMenu=true,
                    Title="Payslip",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Payroll/Payslip",
                },
                new Page{
                    Icon="mdi mdi-account-card-details",
                    Id="LoanRequest",
                    Index=2,
                    PageCode="LoanRequest",
                    ParentId="Payroll",
                    ShowAsMenu=true,
                    Title="Loan Request",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Payroll/LoanRequest",
                },
                new Page{
                    Icon="mdi mdi-settings",
                    Id="LoanConfiguration",
                    Index=3,
                    PageCode="LoanConfiguration",
                    ParentId="Payroll",
                    ShowAsMenu=true,
                    Title="Loan Configuration",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/ConfigLoan",
                },
                new Page{
                    Icon="mdi mdi-calendar-clock",
                    Id="TimeManagement",
                    Index=3,
                    PageCode="TimeManagement",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Time Management",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="",
                    Id="MyAttendance",
                    Index=0,
                    PageCode="MyAttendance",
                    ParentId="TimeManagement",
                    ShowAsMenu=true,
                    Title="My Attendance",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/TimeManagement/MyTimeAttendance",
                },
                new Page{
                    Icon="",
                    Id="SubordinateAttendance",
                    Index=1,
                    PageCode="SubordinateAttendance",
                    ParentId="TimeManagement",
                    ShowAsMenu=true,
                    ForWhomHasSubordinate= true,
                    Title="Subordinate Attendance",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/TimeManagement/SubordinateAttendance",
                },
                new Page{
                    Icon="",
                    Id="Agenda",
                    Index=2,
                    PageCode="Agenda",
                    ParentId="TimeManagement",
                    ShowAsMenu=true,
                    ForWhomHasSubordinate= false,
                    Title="Agenda",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/TimeManagement/Agenda",
                },
                new Page{
                    Icon="mdi mdi-bag-personal",
                    Id="Leave",
                    Index=4,
                    PageCode="Leave",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Leave",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Leave",
                },
                new Page{
                    Icon="",
                    Id="LeaveHistory",
                    Index=0,
                    PageCode="LeaveHistory",
                    ParentId="Leave",
                    Title="Leave History",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="",
                    Id="LeaveSubordinate",
                    Index=1,
                    PageCode="LeaveSubordinate",
                    ParentId="Leave",
                    Title="Leave Subordinate",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="mdi mdi-wallet-travel",
                    Id="Travel",
                    Index=5,
                    PageCode="Travel",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Travel",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Travel",
                },
                //new Page{
                //    Icon="mdi mdi-code-array",
                //    Id="Benefit",
                //    Index=6,
                //    PageCode="Benefit",
                //    ParentId=null,
                //    ShowAsMenu=true,
                //    Title="Benefit",
                //    ShiftType=EmployeeShiftType.All,
                //    Url="#",
                //},
                new Page{
                    Icon="mdi mdi-ambulance",
                    Id="MedicalBenefit",
                    Index=6,
                    PageCode="MedicalBenefit",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="MedicalBenefit",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Benefit/Reimburse",
                },
                new Page{
                    Icon="mdi mdi-food",
                    Id="Canteen",
                    Index=7,
                    PageCode="Canteen",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Canteen",
                    ShiftType=EmployeeShiftType.NonShift,
                    Url="#",
                },
                new Page{
                    Icon="mdi mdi-food",
                    Id="CanteenInformation",
                    Index=1,
                    PageCode="CanteenInformation",
                    ParentId="Canteen",
                    ShowAsMenu=true,
                    Title="Information",
                    ShiftType=EmployeeShiftType.NonShift,
                    Url="/ESS/Canteen",
                },
                new Page{
                    Icon="mdi mdi-store",
                    Id="CanteenManagement",
                    Index=2,
                    PageCode="CanteenManagement",
                    ParentId="Canteen",
                    ShowAsMenu=true,
                    Title="Merchant",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Canteen/Manage",
                },
                new Page{
                    Icon="mdi mdi-chart-areaspline",
                    Id="CanteenReport",
                    Index=3,
                    PageCode="CanteenReport",
                    ParentId="Canteen",
                    ShowAsMenu=true,
                    Title="Report",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Canteen/Report",
                },
                new Page{
                    Icon="mdi mdi-store",
                    Id="CanteenClaimVoucher",
                    Index=4,
                    PageCode="CanteenClaimVoucher",
                    ParentId="Canteen",
                    ShowAsMenu=true,
                    Title="Claim",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Canteen/ClaimVoucher",
                },
                new Page{
                    Icon="mdi mdi-store",
                    Id="CanteenPaymentClaim",
                    Index=5,
                    PageCode="CanteenPaymentClaim",
                    ParentId="Canteen",
                    ShowAsMenu=true,
                    Title="Claim Payment",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Canteen/PaymentClaim",
                },
                new Page{
                    Icon="mdi mdi-run",
                    Id="Training",
                    Index=8,
                    PageCode="Training",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Training",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Training",
                },


                new Page{
                    Icon="mdi mdi-zip-box",
                    Id="ComplaintRequest",
                    Index=9,
                    PageCode="ComplaintRequest",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Complaint & Request",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="mdi mdi-zip-box",
                    Id="Tickets",
                    Index=0,
                    PageCode="Tickets",
                    ParentId="ComplaintRequest",
                    ShowAsMenu=true,
                    Title="Tickets",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Complaint/Index",
                },
                new Page{
                    Icon="mdi mdi-checkbox-multiple-marked-circle-outline",
                    Id="Resolution",
                    Index=1,
                    PageCode="Resolution",
                    ParentId="ComplaintRequest",
                    ShowAsMenu=true,
                    Title="Resolution",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Complaint/Resolution",
                },
                new Page{
                    Icon="mdi mdi-settings",
                    Id="TicketCategory",
                    Index=2,
                    PageCode="TicketCategory",
                    ParentId="ComplaintRequest",
                    ShowAsMenu=true,
                    Title="TicketCategory",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Complaint/TicketCategory",
                },
                new Page{
                    Icon="mdi mdi-calendar-check",
                    Id="Retirement",
                    Index=10,
                    PageCode="Retirement",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Retirement",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Retirement/Index",
                },
                new Page{
                    Icon="mdi mdi-library-books",
                    Id="Recruitment",
                    Index=11,
                    PageCode="Recruitment",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Recruitment",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="mdi mdi-library-books",
                    Id="RecruitmentRequest",
                    Index=0,
                    PageCode="RecruitmentRequest",
                    ParentId="Recruitment",
                    ShowAsMenu=true,
                    Title="Request",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Recruitment/Index",
                },
                new Page{
                    Icon="mdi mdi-library-books",
                    Id="Applications",
                    Index=1,
                    PageCode="Applications",
                    ParentId="Recruitment",
                    ShowAsMenu=true,
                    Title="Applications",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Recruitment/Application",
                },
                new Page{
                    Icon= "mdi mdi-checkbox-multiple-marked-outline",
                    Id= "Task",
                    PageCode= "Task",
                    Title= "Task",
                    Url= "/ESS/Task",
                    ParentId= null,
                    Index= 12,
                    Enabled= true,
                    ForWhomHasSubordinate= false,
                    ShiftType=EmployeeShiftType.All,
                    ShowAsMenu= true,
                },
                 new Page{
                    Icon="mdi mdi-checkbox-multiple-marked-outline",
                    Id="Surveys",
                    Index=13,
                    PageCode="Surveys",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Surveys",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="mdi mdi-checkbox-multiple-blank-outline",
                    Id="Survey",
                    Index=0,
                    PageCode="Survey",
                    ParentId="Surveys",
                    ShowAsMenu=true,
                    Title="Survey",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Survey",
                },
                new Page{
                    Icon="mdi mdi-group",
                    Id="History",
                    Index=1,
                    PageCode="SurveyHistroy",
                    ParentId="Surveys",
                    ShowAsMenu=true,
                    Title="History",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Survey/History",
                },
                new Page{
                    Icon="mdi mdi-settings",
                    Id="Administration",
                    Index=20,
                    PageCode="Administration",
                    ParentId=null,
                    ShowAsMenu=true,
                    Title="Administration",
                    ShiftType=EmployeeShiftType.All,
                    Url="#",
                },
                new Page{
                    Icon="mdi mdi-account",
                    Id="UserAdministration",
                    Index=0,
                    PageCode="UserAdministration",
                    ParentId="Administration",
                    ShowAsMenu=true,
                    Title="User",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/User",
                },
                new Page{
                    Icon="mdi mdi-group",
                    Id="GroupAdministration",
                    Index=1,
                    PageCode="GroupAdministration",
                    ParentId="Administration",
                    ShowAsMenu=true,
                    Title="Group",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Group",
                },
                new Page{
                    Icon="mdi mdi-apps",
                    Id="PageAdministration",
                    Index=1,
                    PageCode="PageAdministration",
                    ParentId="Administration",
                    ShowAsMenu=true,
                    Title="Page",
                    ShiftType=EmployeeShiftType.All,
                    Url="/ESS/Page",
                },
            });


            if (pageCount > 0 && !force) return pages;

            foreach (var page in pages)
            {
                page.Enabled = true;
                page.CreateBy = "system";
                page.CreateDate = DateTime.Now;
                db.Save(page);
            }

            return pages;
        }

    }

    public class MenuPage
    {
        public string PageCode { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public bool ForWhomHasSubordinate { get; set; }

        public string category { get => Url == "#" ? "header" : "menu"; }

        public List<MenuPage> Submenus { get; } = new List<MenuPage>();

        public void MakeSubmenu(Page page, List<Page> pages, Group[] groups)
        {
            if (groups.Where(g => g.Grant.Where(gr => gr.PageCode == page.PageCode && gr.CanRead).Count() > 0).Count() <= 0) return;
            var mp = new MenuPage();
            mp.PageCode = page.PageCode;
            mp.Url = page.Url;
            mp.Title = page.Title;
            mp.Icon = page.Icon;
            mp.ForWhomHasSubordinate = page.ForWhomHasSubordinate;
            foreach (var sm in pages.Where(p => p.ParentId == page.Id))
            {
                mp.MakeSubmenu(sm, pages, groups);
            }
            Submenus.Add(mp);
        }

        public static List<MenuPage> GenerateFromGroups(IMongoDatabase DB, Group[] groups, bool menuOnly = true, bool flat = false)
        {
            var res = new List<MenuPage>();
            var pages = DB.GetCollection<Page>().Find(p => p.Enabled && p.ShowAsMenu == menuOnly).ToList();
            var tld = new List<Page>();

            if (flat)
                tld = pages;
            else
                tld = pages.Where(p => string.IsNullOrEmpty(p.ParentId)).ToList();


            foreach (var tl in tld)
            {
                if (groups.Where(g => g.Grant.Where(gr => gr.PageCode == tl.PageCode && gr.CanRead).Count() > 0).Count() <= 0) continue;
                var mp = new MenuPage();
                mp.PageCode = tl.PageCode;
                mp.Url = tl.Url;
                mp.Title = tl.Title;
                mp.Icon = tl.Icon;
                mp.ForWhomHasSubordinate = tl.ForWhomHasSubordinate;

                if (!flat)
                    foreach (var sm in pages.Where(p => p.ParentId == tl.Id))
                        mp.MakeSubmenu(sm, pages, groups);

                res.Add(mp);
            }
            return res;
        }

        public static List<MenuPage> GenerateFromGroupIds(IMongoDatabase DB, string[] groupids, bool menuOnly = true, bool flat = false)
        {
            groupids = groupids ?? new string[0];
            var groups = DB.GetCollection<Group>().Find(g => groupids.Contains(g.Id)).ToEnumerable().ToArray();
            return GenerateFromGroups(DB, groups, menuOnly, flat);
        }
    }


    public class TieredPageManagement : Page
    {
        public List<Page> Children { get; set; } = new List<Page>();
        public int Level { get; set; }

        public static TieredPageManagement FromPageManagement(Page pm)
        {
            return new TieredPageManagement()
            {
                CreateBy = pm.CreateBy,
                CreateDate = pm.CreateDate,
                Enabled = pm.Enabled,
                Icon = pm.Icon,
                Id = pm.Id,
                Index = pm.Index,
                LastUpdate = pm.LastUpdate,
                PageCode = pm.PageCode,
                ParentId = pm.ParentId,
                ShowAsMenu = pm.ShowAsMenu,
                ForWhomHasSubordinate = pm.ForWhomHasSubordinate,
                SpecialActions = pm.SpecialActions,
                Title = pm.Title,
                UpdateBy = pm.UpdateBy,
                Url = pm.Url
            };
        }
    }

    public enum EmployeeShiftType : int
    {
        [Description("All")]
        All = 1,
        [Description("Shift")]
        Shift = 2,
        [Description("Non Shift")]
        NonShift = 3,
    }

}
