using System;
using Rental.Business.Dtos;

namespace Rental.Business.Interfaces
{
    public interface IAuthService
    {
        ServiceResult RegisterUser(RegisterDto request);
        ServiceResult LoginUser(LoginDto request);
        ServiceResult UpdateProfile(UpdateProfileDto request, Guid userId);
        ServiceResult ChangePassword(ChangePasswordDto request, Guid userId);
        bool CheckIdentifierExists(string identifier);
    }
}
