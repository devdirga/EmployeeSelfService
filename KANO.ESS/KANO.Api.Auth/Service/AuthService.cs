﻿using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using KANO.Core.Service.Odoo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;

namespace KANO.Api.Auth.Service
{
    public class AuthService
    {
        public IConfiguration Configuration { get; }
        public IMongoDatabase DB { get; }
        public IMongoManager Mongo { get; }

        private EmployeeAdapter _employeeAdapter;      

        public AuthService(IMongoManager mongo, IConfiguration config)
        {
            Configuration = config;
            Mongo = mongo;
            DB = mongo.Database();

            _employeeAdapter = new EmployeeAdapter(Configuration);            
        }

        private class UserDetail { 
            public User User { set; get; }
            public Group Group { set; get; }
            public Employee Employee { set; get; }
            public string OdooSessionID { set; get; }
            public bool HasSubordinate { set; get; }
        }

        private UserDetail getUserDetail(string employeeID)
        {
            var userDetail = new UserDetail();
            var tasks = new List<Task<TaskRequest<Exception>>>();

            // Fetch employee data from AX
            tasks.Add(Task.Run(() =>
            {
                Exception error = null;
                try
                {
                    userDetail.Employee = _employeeAdapter.GetDetail(employeeID);

                }
                catch (Exception e)
                {

                    error = e;
                }

                return TaskRequest<Exception>.Create("employee", error);
            }));

            // Fetch employee data from AX
            tasks.Add(Task.Run(() =>
            {
                Exception error = null;
                try
                {
                    userDetail.HasSubordinate = _employeeAdapter.HasSubordinate(employeeID);
                }
                catch (Exception e)
                {

                    error = e;
                }

                return TaskRequest<Exception>.Create("subordinate", error);
            }));

            // Fetch user data from DB
            tasks.Add(Task.Run(() => {
                Exception error = null;
                try
                {
                     var user = DB.GetCollection<User>()
                        .Find(x => x.Username == employeeID)
                        .FirstOrDefault();

                    if (user != null)
                    {
                        userDetail.User = user;

                        var userRole = user.Roles.First();
                        var role = DB.GetCollection<Group>()
                            .Find(x => x.Id == userRole)
                            .FirstOrDefault();
                        userDetail.Group = role;
                    }
                    
                }
                catch (Exception e)
                {
                    error = e;
                }

                return TaskRequest<Exception>.Create("user", error);
            }));

            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                throw e;
            }

            // Combine result
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                {
                    var e = (Exception)r.Result;
                    if (e != null)
                        switch (r.Label)
                        {
                            case "employee":
                                throw new Exception("Unable to get employee from AX", e);
                            case "user":
                                throw new Exception("Unable to get user from ESS", e);
                            case "subordinate":
                                throw new Exception("Unable to get employee subordinate from AX", e);
                            default:
                                break;
                        }
                }               
            }

            return userDetail;

        }

        public AuthResult Auth(string employeeID, string email, string password)
        {
            employeeID=employeeID?.Trim();
            email = email?.Trim();
            password = password?.Trim();


            var res = new AuthResult();

            var userDetail = this.getUserDetail(employeeID);
            var user = userDetail.User;            
            var group = userDetail.Group;            
            var employee = userDetail.Employee;

            if (group != null && !group.Enable) {
                res.Message = "User group is not authorized to access app";
                return res;
            }

            if (user != null)
            {
                if (!user.Enable) { 
                    res.Message = "User group is not authorized to access app";
                    return res;
                }
                else if (!string.IsNullOrWhiteSpace(user.PasswordHash) && user.VerifyPassword(password) != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                {
                    bool hasSubordinate = userDetail.HasSubordinate;
                    user.UserData["hasSubordinate"] = hasSubordinate.ToString();
                    if (employee != null)
                    {
                        user.UserData["lastEmploymentDate"] = employee.LastEmploymentDate.ToString("yyyy-MM-dd");
                        user.UserData["profilePicture"] = Tools.FileToBase64(employee.ProfilePicture);
                        user.UserData["workerType"] = employee.WorkerTimeType;
                    }
                    else 
                    {
                        user.UserData["lastEmploymentDate"] = default(DateTime).ToString("yyyy-MM-dd");
                        user.UserData["profilePicture"] = "";
                        user.UserData["workerType"] = "";
                    }
                    res.AuthState = AuthState.Authenticated;
                    res.Success = true;
                    res.User = user;

                    if (user.UserData["workerType"] == "NS")
                    {
                        generateOnLogin(employeeID);
                    }
                    
                    return res;                                        
                }
                else
                {
                    res.Message = "Invalid password or employee id";
                    return res;
                }
            }

            // Fetch User From External Database            
            if (employee != null)
            {
                res.AuthState = AuthState.NeedActivation;
                res.Message = "User need to be activated";

                return res;
            }
            
            res.Message = "Invalid employee id";
            return res;
        }

        public AuthResult Activate(string token, string newPassword)
        {
            var res = new AuthResult();

            // Find Local First
            var actRec = DB.GetCollection<ActivationRecord>()
                .Find(r => r.Id == token)
                .FirstOrDefault();

            if (actRec == null || (actRec!= null && actRec.ExpiredOn.ToLocalTime() < DateTime.Now))
            {
                if(actRec != null) DB.Delete(actRec); 

                res.Message = "Activation token is invalid or has expired";
                return res;
            }

            var userDetail = this.getUserDetail(actRec.Employee.EmployeeID);
            var user = userDetail.User;
            bool hasSubordinate = userDetail.HasSubordinate;

            if (user != null)
            {
                DB.Delete(actRec);
                res.Message = "User is already activated";
                return res;
            }
            

            // Create the user
            var newUser = new User();
            newUser.Id = actRec.Employee.EmployeeID;
            newUser.Email = actRec.Email;
            newUser.Username = actRec.Employee.EmployeeID;
            newUser.FullName = actRec.Employee.EmployeeName;
            newUser.NewPassword = newPassword;
            newUser.Roles = new List<string>();
            newUser.Roles.Add("CommonEmployee");
            newUser.UserData["gender"]=actRec.Employee.Gender.ToString();
            newUser.UserData["hasSubordinate"]= hasSubordinate.ToString();
            newUser.Enable = true;

            if (userDetail.Employee != null){
                newUser.UserData["lastEmploymentDate"] = userDetail.Employee?.LastEmploymentDate.ToString("yyyy-MM-dd");
            }else{
                newUser.UserData["lastEmploymentDate"] = default(DateTime).ToString("yyyy-MM-dd");
            }
            
            DB.Save(newUser);

            DB.Delete(actRec);

            res.Success = true;
            res.User = newUser;
            res.Message = "User is activated successfully";
            return res;
        }               

        public AuthResult SendMailActivationUser(string employeeid, string email, string baseURL)
        {
            email = email?.Trim().ToLower();
            employeeid = employeeid?.Trim();
            var res = new AuthResult();
            var user = DB.GetCollection<User>()
               .Find(x => x.Username == employeeid || (x.Email == email && x.Email != ""))
               .FirstOrDefault();

            if (user != null)
            {
                // TODO
                // Plesase remove this code for safety
                res.User = user;
                res.Message = "user is already activated";
                return res;
            }

            // Fetch User From External Database                                    
            Employee employee = null; 
            
            try
            {
                employee = _employeeAdapter.Get(employeeid);               
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = $"Unable to fetch employee data {Format.ExceptionString(e)}";
                return res;
            }

            var electronicAddresses = new List<ElectronicAddress>();
            try
            {
                electronicAddresses = _employeeAdapter.GetElectronicAddresses(employeeid);
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = $"Unable to fetch employee electronic address {Format.ExceptionString(e)}";
                return res;
            }            

            if (electronicAddresses.Count == 0) {
                res.Success = false;
                res.Message = $"Employee electronic address is empty";
                return res;
            }

            var employeeEmail = electronicAddresses.Find(x => x.Type == KESSHCMServices.LogisticsElectronicAddressMethodType.Email && x.Locator.Trim().ToLower() == email);            
            if (employee == null)
            {
                res.Success = false;
                res.Message = "Invalid employee id";
            }
            else if (employeeEmail == null || (employeeEmail != null && string.IsNullOrWhiteSpace(employeeEmail.Locator)))
            {
                res.Success = false;
                res.Message = "Email does not match with our data";
            }
            else
            {
                try
                {
                    ActivationRecord rec;
                    User usr = new User();
                    var actRec = DB.GetCollection<ActivationRecord>()
                        .Find(r => r.Email == email && r.Employee.EmployeeID == employeeid)
                        .FirstOrDefault();

                    if (actRec != null && actRec.ExpiredOn.ToLocalTime() >= DateTime.Now)
                    {
                        rec = actRec;
                    }
                    else
                    {
                        rec = new ActivationRecord()
                        {
                            Email = email,
                            Employee = employee,
                            ExpiredOn = DateTime.Now.AddDays(1),
                            Id = Guid.NewGuid().ToString("N")
                        };

                        DB.Save(rec);
                    }

                    string ActivationLink = $"{baseURL}/{rec.Id}";
                    res.Success = true;
                    res.Data = ActivationLink;
                    res.Message = $"{rec.Employee.EmployeeName}";
                }
                catch (Exception e)
                {
                    res.Success = false;
                    res.Message = e.Message.ToString();
                    return res;
                }
                
            }
            return res;
        }

        public AuthResult SendResetPassword(string email)
        {
            var res = new AuthResult();
            email = email?.Trim().ToLower();

            // Validate email on ess user collection
            var user = DB.GetCollection<User>().Find(x => x.Email == email).ToList().FirstOrDefault();
            if (user == null)
            {
                res.Message = "Invalid email";
                return res;
            }
            else if (user.Email.Trim() == "" || user.Email == null)
            {
                res.Message = "User doesn't have valid email";
                return res;
            }
           
            try
            {
                ResetPasswordRecord rec;
                User usr = new User();
                var resPassRec = DB.GetCollection<ResetPasswordRecord>()
                    .Find(r => r.Email == email)
                    .FirstOrDefault();
                
                if (resPassRec != null && resPassRec.ExpiredOn.ToLocalTime() >= DateTime.Now)
                {
                    rec = resPassRec;
                }
                else
                {
                    rec = new ResetPasswordRecord()
                    {
                        Email = email.Trim(),
                        Users = user,
                        ExpiredOn = DateTime.Now.AddHours(12),
                        Id = Guid.NewGuid().ToString("N")
                    };

                    DB.Save(rec);
                }                


                // Setting reset password result
                res.Success = true;
                // TODO
                // Please remove this code in order to safety
                res.User = user;
                res.Data = rec.Id;
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = e.Message.ToString();
            }
            return res;
        }


        public AuthResult ResetPassword(string strToken, string password)
        {
            // Verify Token
            var verification = this.VerifyResetPasswordKey(strToken);
            if (!verification.Success)
            {
                return verification;
            }

            // Saving new password
            var res = new AuthResult();
            var user = verification.User;
            user.NewPassword = password;
            DB.Save(user);

            
            // Setting reset password result
            res.Success = true;
            res.User = user;                
            return res;            
        }

        public AuthResult VerifyResetPasswordKey(string key)
        {
            var res = new AuthResult();
            var check = DB.GetCollection<ResetPasswordRecord>().Find(x => x.Id == key).FirstOrDefault();

            if (check == null || (check != null && check.ExpiredOn.ToLocalTime() < DateTime.Now))
            {
                if (check!= null) DB.Delete(check);

                res.Message = "Reset password token is invalid or expired";
                res.Success = false;                
            }
            else
            {
                res.Message = "Valid token";
                res.User = check.Users;
                res.Success = true;
            }
            
            return res;
        }

        public AuthResult ChangePassword(string employeeID, string currentPassword, string newPassword) {
            var res = new AuthResult();

            // Find User on Local Database
            var user = DB.GetCollection<User>()
                .Find(x => x.Username == employeeID)
                .FirstOrDefault();

            if (user != null)
            {
                if (user.VerifyPassword(currentPassword) != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                {
                    user.NewPassword = newPassword;
                    DB.Save(user);

                    res.Message = "Password has been changed successfully";
                    res.Success = true;
                    res.User = user;
                    return res;
                }
                else
                {
                    res.Message = "Invalid current password";
                    return res;
                }
            }

            res.Message = "Unable to find user";
            return res;
        }

        private bool generateOnLogin(string employeeID)
        {
            try
            {
                DateRange range = new DateRange();
                range.Start = DateTime.Now;
                range.Finish = DateTime.Now;
                var adapter = new TimeManagementAdapter(this.Configuration);

                // check absen to AX
                var absence = adapter.GetAbsenceImported(employeeID);
                if (absence.Where(x => x.PresenceDateField == DateTime.Now).Count() != 0)
                {
                    if (checkVoucher(employeeID))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }                
            }
            catch (Exception e)
            {
                Debug.WriteLine("error:" + e);
                return false;
            }
        }

        private bool checkVoucher(string employeeID)
        {
            try
            {
                var now = DateTime.Now;
                var nowDate = new DateTime(now.Year, now.Month, now.Day, 0 , 0, 0 , DateTimeKind.Utc);
                var generatedDateFor = nowDate;
                var employee = DB.GetCollection<User>().Find(z => z.Id == employeeID).FirstOrDefault();
                var v = DB.GetCollection<Voucher>().Find(x => x.EmployeeID == employeeID && x.GeneratedForDate == generatedDateFor).FirstOrDefault();
                if (v == null)
                {
                    var voucher = new Voucher
                    {
                        EmployeeID = employeeID,
                        EmployeeName = employee.FullName,
                        GeneratedForDate = generatedDateFor,
                    };

                    DB.Save(voucher);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
