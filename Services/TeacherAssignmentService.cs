// using Microsoft.EntityFrameworkCore;
// using UserManagementApi.Data;
// using UserManagementApi.Models;
// using UserManagementApi.Services.Interfaces;

// namespace UserManagementApi.Services;

// public class TeacherAssignmentService : ITeacherAssignmentService
// {
//     private readonly ApplicationDbContext _context;

//     public TeacherAssignmentService(
//         ApplicationDbContext context)
//     {
//         _context = context;
//     }

//     public async Task<(bool Success, object? Data, string? Error)>
//         AssignClassAsync(
//             Guid schoolId,
//             Guid teacherId,
//             Guid classId)
//     {
//         var teacher = await _context.Teachers
//             .FirstOrDefaultAsync(x =>
//                 x.Id == teacherId &&
//                 x.SchoolId == schoolId);

//         if (teacher == null)
//         {
//             return (false, null, "Teacher not found.");
//         }

//         var classroom = await _context.Classes
//             .FirstOrDefaultAsync(x =>
//                 x.Id == classId &&
//                 x.SchoolId == schoolId);

//         if (classroom == null)
//         {
//             return (
//                 false,
//                 null,
//                 "Class not found."
//             );
//         }

//         var exists = await _context.TeacherClasses
//             .AnyAsync(x =>
//                 x.TeacherId == teacherId &&
//                 x.ClassId == classId);

//         if (exists)
//         {
//             return (
//                 false,
//                 null,
//                 "Teacher is already assigned to this class."
//             );
//         }

//         var assignment = new TeacherClass
//         {
//             TeacherId = teacherId,
//             ClassId = classId
//         };

//         _context.TeacherClasses.Add(assignment);

//         await _context.SaveChangesAsync();

//         return (
//             true,
//             new
//             {
//                 TeacherId = teacher.Id,
//                 TeacherName = teacher.User.FullName,
//                 ClassId = classroom.Id,
//                 ClassName = classroom.Name
//             },
//             null
//         );
//     }

//     public async Task<(bool Success, object? Data, string? Error)>
//         AssignSubjectAsync(
//             Guid schoolId,
//             Guid teacherId,
//             Guid subjectId)
//     {
//         var teacher = await _context.Teachers
//             .FirstOrDefaultAsync(x =>
//                 x.Id == teacherId &&
//                 x.SchoolId == schoolId);

//         if (teacher == null)
//         {
//             return (false, null, "Teacher not found.");
//         }

//         var subject = await _context.Subjects
//             .FirstOrDefaultAsync(x =>
//                 x.Id == subjectId &&
//                 x.SchoolId == schoolId);

//         if (subject == null)
//         {
//             return (
//                 false,
//                 null,
//                 "Subject not found."
//             );
//         }

//         var exists = await _context.TeacherSubjects
//             .AnyAsync(x =>
//                 x.TeacherId == teacherId &&
//                 x.SubjectId == subjectId);

//         if (exists)
//         {
//             return (
//                 false,
//                 null,
//                 "Teacher is already assigned to this subject."
//             );
//         }

//         var assignment = new TeacherSubject
//         {
//             TeacherId = teacherId,
//             SubjectId = subjectId
//         };

//         _context.TeacherSubjects.Add(assignment);

//         await _context.SaveChangesAsync();

//         return (
//             true,
//             new
//             {
//                 TeacherId = teacher.Id,
//                 TeacherName = teacher.User.FullName,
//                 SubjectId = subject.Id,
//                 SubjectName = subject.Name
//             },
//             null
//         );
//     }

//     public async Task<(bool Success, string? Error)>
//         RemoveClassAsync(
//             Guid schoolId,
//             Guid teacherId,
//             Guid classId)
//     {
//         var assignment =
//             await _context.TeacherClasses
//                 .FirstOrDefaultAsync(x =>
//                     x.TeacherId == teacherId &&
//                     x.ClassId == classId &&
//                     x.Teacher.SchoolId == schoolId);

//         if (assignment == null)
//         {
//             return (
//                 false,
//                 "Teacher is not assigned to this class."
//             );
//         }

//         _context.TeacherClasses.Remove(assignment);

//         await _context.SaveChangesAsync();

//         return (true, null);
//     }

//     public async Task<(bool Success, string? Error)>
//         RemoveSubjectAsync(
//             Guid schoolId,
//             Guid teacherId,
//             Guid subjectId)
//     {
//         var assignment =
//             await _context.TeacherSubjects
//                 .FirstOrDefaultAsync(x =>
//                     x.TeacherId == teacherId &&
//                     x.SubjectId == subjectId &&
//                     x.Teacher.SchoolId == schoolId);

//         if (assignment == null)
//         {
//             return (
//                 false,
//                 "Teacher is not assigned to this subject."
//             );
//         }

//         _context.TeacherSubjects.Remove(assignment);

//         await _context.SaveChangesAsync();

//         return (true, null);
//     }
// }