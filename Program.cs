using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Models;
using UserManagementApi.Data;
using UserManagementApi.Services;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Resend;
using UserManagementApi.Services.Interfaces;
using UserManagementApi.Configuration;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!
                ))
        };
    });

builder.Services.AddOptions();
builder.Services.Configure<ResendClientOptions>(
    builder.Configuration.GetSection("Resend"));

builder.Services.AddHttpClient<ResendClient>();
builder.Services.AddTransient<IResend, ResendClient>();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RolesService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICurrentUserService, UserService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ICurrentUserService, UserService>();
builder.Services.AddScoped<
    ITeacherClassService,
    TeacherClassService>();
builder.Services.AddScoped<
ITeacherSubjectService,
TeacherSubjectService>();
builder.Services.AddScoped<
    IClassService,
    ClassService>();
builder.Services.AddScoped<
ISubjectService,
SubjectService>();
// builder.Services.AddScoped<
// ITeacherAssignmentService,
// TeacherAssignmentService>();
builder.Services.AddScoped<
IParentService,
ParentService>();
builder.Services.AddScoped<
IParentPortalService,
ParentPortalService>();
builder.Services.AddScoped<
IStudentPortalService,
StudentPortalService>();
builder.Services.AddScoped<
IResultGradingService,
ResultGradingService>();
builder.Services.AddScoped<
    ITeacherPortalService,
    TeacherPortalService>();
builder.Services.AddScoped<
IAcademicSessionService,
AcademicSessionService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITeacherPortalService, TeacherPortalService>();
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("my-job");

    q.AddJob<MyJob>(options =>
        options.WithIdentity(jobKey));

    q.AddTrigger(options => options
        .ForJob(jobKey)
        .WithIdentity("keep-alive-trigger")
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

builder.Services.Configure<SeedAdminSettings>(
    builder.Configuration.GetSection("SeedAdmin"));

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var rolesService = scope.ServiceProvider.GetRequiredService<RolesService>();

    // await rolesService.EnsureRolesExistAsync();
    await IdentitySeeder.SeedAsync(
        scope.ServiceProvider);
}

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
