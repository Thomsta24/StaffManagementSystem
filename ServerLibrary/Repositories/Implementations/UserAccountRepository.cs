using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerLibrary.Data;
using ServerLibrary.Helpers;
using ServerLibrary.Repositories.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Constants = ServerLibrary.Helpers.Constants;

namespace ServerLibrary.Repositories.Implementations
{
    public class UserAccountRepository : IUserAccount
    {
        #region Fields

        private readonly IOptions<JwtSection> _config;
        private readonly AppDbContext _appDbContext;

        #endregion

        #region Ctor

        public UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbContext)
        {
            _config = config;
            _appDbContext = appDbContext;
        }

        #endregion

        public async Task<GeneralResponse> CreateAsync(Register user)
        {
            if (user == null) return new GeneralResponse(false, "Model is empty");

            var checkUser = await GetUserByEmail(user.Email!);
            if (checkUser != null) return new GeneralResponse(false, "User registered already");

            //Save user
            var applicationUser = await AddToDatabase(new ApplicationUser()
            {
                Fullname = user.Fullname,
                Email = user.Email ,
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
            });

            //check, create and assign role
            var checkAdminRole = await _appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.Admin));
            if(checkAdminRole == null)
            {
                var createAdminRole = await AddToDatabase(new SystemRole(){ Name = Constants.Admin });
                await AddToDatabase(new UserRole() { RoleId = createAdminRole.Id, UserId = applicationUser.Id });
                return new GeneralResponse(true, "Account created!");
            }

            var checkUserRole = await _appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.User));
            SystemRole response = new();
            if (checkUserRole == null)
            {
                response = await AddToDatabase(new SystemRole() { Name = Constants.User });
                await AddToDatabase(new UserRole() { RoleId = response.Id, UserId = applicationUser.Id });
            }
            else
            {
                await AddToDatabase(new UserRole() { RoleId = checkUserRole.Id, UserId = applicationUser.Id });
            }
            return new GeneralResponse(true, "Account created!");
        }

        public async Task<LoginResponse> SignInAsync(Login user)
        {
            if (user == null) return new LoginResponse(false, "Model is empty");

            var applicationUser = await GetUserByEmail(user.Email!);
            if (applicationUser == null) return new LoginResponse(false, "User not found");

            //Verify password
            if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
                return new LoginResponse(false, "Email/Password not valid");

            var getUserRole = await GetUserRole(applicationUser.Id);
            if (getUserRole == null) return new LoginResponse(false, "User role not found");

            var getRoleName = await GetRoleName(getUserRole.RoleId);
            if (getUserRole == null) return new LoginResponse(false, "User role not found");

            string jwtToken = GenerateToken(applicationUser, getRoleName!.Name!);
            string refreshToken = GenerateRefreshToken();

            //Save to Refresh token to the database
            var getUser = await _appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == applicationUser.Id);
            if (getUser != null)
            {
                getUser.Token = refreshToken;
                await _appDbContext.SaveChangesAsync();
            }
            else
            {
                await AddToDatabase(new RefreshTokenInfo() { Token = refreshToken, UserId = applicationUser.Id });
            }

            return new LoginResponse(true, "Login successfully", jwtToken, refreshToken);
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
        {
            if (token == null) return new LoginResponse(false, "Model is empty");

            var getToken = await _appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.Token!.Equals(token.Token));
            if (getToken == null) return new LoginResponse(false, "Refresh token is required");

            //get user details
            var user = await _appDbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Id == getToken.UserId);
            if (user == null) return new LoginResponse(false, "Refresh token could not be generated because user not found");

            var userRole = await GetUserRole(user.Id);
            var roleName = await GetRoleName(userRole.RoleId);
            string jwtToken = GenerateToken(user, roleName.Name!);
            string refreshToken = GenerateRefreshToken();

            var updateRefreshToken = await _appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.UserId == user.Id);
            if (updateRefreshToken == null) return new LoginResponse(false, "Refresh token could not be generated because user has not sign in");

            updateRefreshToken.Token = refreshToken;
            await _appDbContext.SaveChangesAsync();
            return new LoginResponse(true, "Token refreshed successfully", jwtToken, refreshToken);
        }

        private async Task<UserRole> GetUserRole(int userId) => await _appDbContext.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId);
        private async Task<SystemRole> GetRoleName(int roleId) => await _appDbContext.SystemRoles.FirstOrDefaultAsync(x => x.Id == roleId);

        private string GenerateToken(ApplicationUser user, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Value.Key!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Fullname!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role!)
            };

            var token = new JwtSecurityToken(
                issuer: _config.Value.Issuer,
                audience: _config.Value.Audience,
                claims: userClaims,
                expires: DateTime.Now.AddSeconds(2),
                signingCredentials: credentials
                );
            
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        private async Task<ApplicationUser> GetUserByEmail(string email) =>
            await _appDbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Email!.ToLower()!.Equals(email!.ToLower()));

        private async Task<T> AddToDatabase<T>(T model)
        {
            var result = _appDbContext.Add(model!);
            await _appDbContext.SaveChangesAsync();
            return (T)result.Entity;
        }

        public async Task<List<ManageUser>> GetUsers()
        {
            var allUsers = await GetApplicationUsers();
            var allUserRoles = await UserRoles();
            var allRoles = await SystemRoles();

            if (allUsers.Count == 0 || allRoles.Count == 0) return null!;

            var users = new List<ManageUser>();
            foreach ( var user in allUsers)
            {
                var userRole = allUserRoles.FirstOrDefault(u => u.UserId == user.Id);
                var roleName = allRoles.FirstOrDefault(r => r.Id == userRole!.RoleId);
                users.Add(new ManageUser() { UserId = user.Id, Name = user.Fullname!, Email = user.Email!, Role = roleName!.Name! });
            }

            return users;
        }

        public async Task<GeneralResponse> UpdateUser(ManageUser user)
        {
            var getRole = (await SystemRoles()).FirstOrDefault(r => r.Name!.Equals(user.Role));
            var userRole = await _appDbContext.UserRoles.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            userRole!.RoleId = getRole!.Id;
            await _appDbContext.SaveChangesAsync();
            return new GeneralResponse(true, "User role updated successfully");
        }

        public async Task<List<SystemRole>> GetRoles() => await SystemRoles();

        public async Task<GeneralResponse> DeleteUser(int id)
        {
            var user = await _appDbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);
            _appDbContext.ApplicationUsers.Remove(user!);
            await _appDbContext.SaveChangesAsync();
            return new GeneralResponse(true, "User deleted successfully");
        }

        private async Task<List<SystemRole>> SystemRoles() => await _appDbContext.SystemRoles.AsNoTracking().ToListAsync();
        private async Task<List<UserRole>> UserRoles() => await _appDbContext.UserRoles.AsNoTracking().ToListAsync();
        private async Task<List<ApplicationUser>> GetApplicationUsers() => await _appDbContext.ApplicationUsers.AsNoTracking().ToListAsync();

        public async Task<string> GetUserImage(int id) => (await GetApplicationUsers()).FirstOrDefault(u => u.Id == id)!.Image;

        public async Task<bool> UpdateProfile(UserProfile profile)
        {
            var user = await _appDbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == int.Parse(profile.Id));

            user!.Email = profile.Email;
            user.Fullname = profile.Name;
            user.Image = profile.Image;

            await _appDbContext.SaveChangesAsync();
            return true;
        }
    }
}
