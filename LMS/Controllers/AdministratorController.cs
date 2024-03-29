﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LMS.Controllers
{
    public class AdministratorController : Controller
    {

        //If your context class is named something different,
        //fix this member var and the constructor param
        private readonly LMSContext db;

        public AdministratorController(LMSContext _db)
        {
            db = _db;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Department(string subject)
        {
            ViewData["subject"] = subject;
            return View();
        }

        public IActionResult Course(string subject, string num)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }

        /*******Begin code to modify********/

        /// <summary>
        /// Create a department which is uniquely identified by it's subject code
        /// </summary>
        /// 
        /// <param name="subject">the subject code</param>
        /// <param name="name">the full name of the department</param>
        /// 
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the department already exists, true otherwise.</returns>
        /// 
        public IActionResult CreateDepartment(string subject, string name)
        {

            bool newDepartment = true;

            var query = from d in db.Departments select d;

            foreach (var departments in query)
            {
                //Check if Department already exists
                if (departments.SubjectAbb == subject || departments.Name == name)
                {
                    Console.WriteLine("Department already exists.");
                    // Department already exists
                    newDepartment = false;
                }
            }

            if (newDepartment)
            {
                Department department = new Department();
                department.SubjectAbb = subject;
                department.Name = name;

                db.Departments.Add(department);
                db.SaveChanges();
            }
            
            return Json(new { success = newDepartment});
        }


        /// <summary>
        /// Returns a JSON array of all the courses in the given department.
        /// 
        /// Each object in the array should have the following fields:
        /// "number" - The course number (as in 5530)
        /// "name" - The course name (as in "Database Systems")
        /// 
        /// </summary>
        /// 
        /// <param name="subjCode">The department subject abbreviation (as in "CS")</param>
        /// 
        /// <returns>The JSON result</returns>
        /// 
        public IActionResult GetCourses(string subject)
        {

            var query = from c in db.Courses
                        where c.SubjectAbb == subject
                        select new
                        {
                            number = c.CourseNumber,
                            name = c.CourseName
                        };

            return Json(query.ToArray());
        }

        /// <summary>
        /// Returns a JSON array of all the professors working in a given department.
        /// 
        /// Each object in the array should have the following fields:
        /// "lname" - The professor's last name
        /// "fname" - The professor's first name
        /// "uid" - The professor's uid
        /// 
        /// </summary>
        /// 
        /// <param name="subject">The department subject abbreviation</param>
        /// 
        /// <returns>The JSON result</returns>
        /// 
        public IActionResult GetProfessors(string subject)
        {
            var query = from p in db.Professors
                //where p.SubjectAbbs.Any(dept => dept.Name == subject)
                join u in db.Users on p.UId equals u.UId
                where p.SubjectAbb == subject
                        select new
                        {
                            lname = u.LastName,
                            fname = u.FirstName,
                            uid = u.UId
                        };

            return Json(query.ToArray());
            
        }



        /// <summary>
        /// Creates a course.
        /// A course is uniquely identified by its number + the subject to which it belongs
        /// </summary>
        /// 
        /// <param name="subject">The subject abbreviation for the department in which the course will be added</param>
        /// <param name="number">The course number</param>
        /// <param name="name">The course name</param>
        /// 
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the course already exists, true otherwise.</returns>
        /// 
        public IActionResult CreateCourse(string subject, int number, string name)
        {
            
            bool newCourse = true;
            
            if (number is < 1000 or > 9999)
            {
                newCourse = false;

            }

            var query = from c in db.Courses select c;

            foreach (var courses in query)
            {
                //Check if Course already exists
                if (courses.SubjectAbb == subject && courses.CourseName == name && courses.CourseNumber == number.ToString())
                {
                    // Course already exists
                    newCourse = false;
                }
            }


            uint catIdCount = 1;


            // Check if database has entries
            if (db.Courses.Any())
            {
                var last = db.Courses.OrderBy(u => u.CatalogId).Last(); // Check last entry
                var id = last.CatalogId;
                catIdCount += id;
            }



            if (newCourse)
            {
                try
                {
                    Course course = new Course();
                    course.SubjectAbb = subject;
                    course.CourseName = name;
                    course.CatalogId = catIdCount;
                    course.CourseNumber = number.ToString();

                    db.Courses.Add(course);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    newCourse = false;
                }
            }

            return Json(new { success = newCourse });
        }



        /// <summary>
        /// Creates a class offering of a given course.
        /// </summary>
        /// 
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="number">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        /// <param name="location">The location</param>
        /// <param name="instructor">The uid of the professor</param>
        /// 
        /// <returns>A JSON object containing {success = true/false}. 
        /// false if another class occupies the same location during any time 
        /// within the start-end range in the same semester, or if there is already
        /// a Class offering of the same Course in the same Semester,
        /// true otherwise.</returns>
        /// 
        public IActionResult CreateClass(string subject, int number, string season, int year, DateTime start, DateTime end, string location, string instructor)
        {
            bool addClassToDB = true;

            if (year is < 1 or > 9999)
            {
                addClassToDB = false;
                return Json(new { success = addClassToDB });
            }

            var query = from cl in db.Classes select cl;

            var catalogID = (from co in db.Courses
                             where co.SubjectAbb == subject
                             where co.CourseNumber == number.ToString()
                             select co.CatalogId).FirstOrDefault();

            // Console.WriteLine("Checking if input details are valid.");
            if (query.Any())
            {
                foreach (var cl in query)
                {

                    if (
                        // Class has a clashing location
                        (cl.Location.Equals(location) &&
                         TimeSpan.Compare(cl.StartTime.ToTimeSpan(), start.TimeOfDay) >= 0 &&
                         TimeSpan.Compare(cl.StartTime.ToTimeSpan(), end.TimeOfDay) <= 0 && cl.Season.Equals(season) &&
                         cl.Year == year &&
                         cl.Season == season)
                        ||
                        // Class has already been defined, but why arent we allowed to have multiple class offerings of the same course in the same semester?
                        (cl.Season == season && cl.Year == year && cl.CatalogId == catalogID)
                    )
                    {
                        Console.WriteLine("Cannot add class.");
                        // Class already exists
                        addClassToDB = false;
                    }
                }
            }
            

            
            uint idCount = 1;


            // Check if database has entries
            if (db.Classes.Any())
            {
                var last = db.Classes.OrderBy(u => u.ClassId).Last(); // Check last entry
                var id = last.ClassId;
                idCount += id;
            }
            


            if (addClassToDB)
            {
                Class new_class = new Class();

                new_class.Location = location;
                new_class.StartTime = TimeOnly.FromDateTime(start);
                new_class.EndTime = TimeOnly.FromDateTime(end);
                new_class.Season = season;
                new_class.Year = (uint) year;
                new_class.CatalogId = catalogID;
                new_class.ClassId = idCount;
                new_class.ProfessorUId = instructor;


                db.Classes.Add(new_class);
                db.SaveChanges();
            }

            return Json(new { success = addClassToDB });

        }

        /*******End code to modify********/

    }
}

