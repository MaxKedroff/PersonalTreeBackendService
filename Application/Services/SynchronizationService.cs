using Application.Dtos;
using Application.Interfaces;
using Core.Utils;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SynchronizationService : ISynchronizationService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SynchronizationService> _logger;
        private readonly IUserRepository _userRepository;

        public SynchronizationService(
            IUnitOfWork unitOfWork,
            ILogger<SynchronizationService> logger,
            IUserRepository userRepository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userRepository = userRepository;
        }

        public async Task<SynchroResponseDto> SyncData(SynchroRequestDto dto)
        {
            if (dto.isHardSynchronize)
                return await HardSyncData(dto);

            return await SoftSyncData(dto);
        }

        private async Task<SynchroResponseDto> SoftSyncData(SynchroRequestDto dto)
        {
            var response = new SynchroResponseDto();

            try
            {
                _logger.LogInformation("Начало мягкой синхронизации. Пользователей для обработки: {Count}", dto.count);
                var dbUsers = await _userRepository.GetUsersAsync();
                var dbUsersByAdGuid = dbUsers
                    .Where(u => !string.IsNullOrEmpty(u.AdGuid))
                    .ToDictionary(u => u.AdGuid, u => u);

                var dbUsersBySamAccount = dbUsers
                    .Where(u => !string.IsNullOrEmpty(u.SamAccountName))
                    .ToDictionary(u => u.SamAccountName, u => u);

                var managerMap = new Dictionary<string, Guid>();
                foreach (var user in dbUsers)
                {
                    if (!string.IsNullOrEmpty(user.AdGuid))
                    {
                        managerMap[user.AdGuid] = user.Manager_id.Value;
                    }
                }

                foreach (var userDto in dto.users)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(userDto.AdGuid))
                        {
                            _logger.LogWarning("Пропуск пользователя без AD GUID: {SamAccountName}", userDto.SamAccountName);
                            continue;
                        }
                        if (dbUsersByAdGuid.TryGetValue(userDto.AdGuid, out var existingUser))
                        {
                            _logger.LogWarning("юзер существует", userDto.SamAccountName);
                        }
                        else
                        {
                            //if (dbUsersBySamAccount.TryGetValue(userDto.SamAccountName, out var userBySam))
                            //{
                            //    userBySam.AdGuid = userDto.AdGuid;
                            //    await UpdateUser(userBySam, userDto, managerMap);
                            //    response.UpdatedUsers++;
                            //}
                            await CreateNewUser(userDto, managerMap);
                            response.AddedUsers++;
                        }

                        response.TotalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при обработке пользователя {SamAccountName}", userDto.SamAccountName);
                        response.Errors.Add($"Ошибка обработки пользователя {userDto.SamAccountName}: {ex.Message}");
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation(
                    "Мягкая синхронизация завершена. Добавлено: {Added}, Обновлено: {Updated}, Обработано: {Total}",
                    response.AddedUsers, response.UpdatedUsers, response.TotalProcessed);

                response.Status = "success";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при мягкой синхронизации");
                response.Status = "error";
                response.Errors.Add($"Критическая ошибка: {ex.Message}");
            }
            return response;
        }

        public async Task<SynchroResponseDto> HardSyncData(SynchroRequestDto dto)
        {
            var response = new SynchroResponseDto();

            try
            {
                _logger.LogInformation("Начало жесткой синхронизации. Пользователей для обработки: {Count}", dto.count);

                var dbUsers = await _userRepository.GetUsersAsync();
                var dbUsersByAdGuid = dbUsers
                    .Where(u => !string.IsNullOrEmpty(u.AdGuid))
                    .ToDictionary(u => u.AdGuid, u => u);

                var dbUsersBySamAccount = dbUsers
                    .Where(u => !string.IsNullOrEmpty(u.SamAccountName))
                    .ToDictionary(u => u.SamAccountName, u => u);

                var requestAdGuids = new HashSet<string>(
                    dto.users.Where(u => !string.IsNullOrEmpty(u.AdGuid))
                           .Select(u => u.AdGuid));

                var managerMap = new Dictionary<string, Guid>();
                foreach (var user in dbUsers)
                {
                    if (!string.IsNullOrEmpty(user.AdGuid))
                    {
                        managerMap[user.AdGuid] = user.User_id;
                    }
                }

                foreach (var dbUser in dbUsers)
                {
                    if (string.IsNullOrEmpty(dbUser.AdGuid))
                        continue;

                    if (!requestAdGuids.Contains(dbUser.AdGuid))
                    {
                        if (dbUser.IsActive)
                        {
                            dbUser.IsActive = false;
                            dbUser.Updated_at = DateTime.UtcNow;
                            _logger.LogInformation("Деактивация пользователя: {SamAccountName}", dbUser.SamAccountName);
                            response.DeactivatedUsers++;
                        }
                        else
                        {
                            _userRepository.Delete(dbUser);
                            _logger.LogInformation("Удаление пользователя: {SamAccountName}", dbUser.SamAccountName);
                            response.DeletedUsers++;
                        }
                    }
                }

                foreach (var userDto in dto.users)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(userDto.AdGuid))
                        {
                            _logger.LogWarning("Пропуск пользователя без AD GUID: {SamAccountName}", userDto.SamAccountName);
                            continue;
                        }

                        if (dbUsersByAdGuid.TryGetValue(userDto.AdGuid, out var existingUser))
                        {
                            // Активируем пользователя если был деактивирован
                            if (!existingUser.IsActive)
                            {
                                existingUser.IsActive = true;
                                response.DeactivatedUsers--;
                            }

                            await UpdateUser(existingUser, userDto, managerMap);
                            response.UpdatedUsers++;
                        }
                        else
                        {
                            if (dbUsersBySamAccount.TryGetValue(userDto.SamAccountName, out var userBySam))
                            {
                                userBySam.AdGuid = userDto.AdGuid;
                                userBySam.IsActive = true;
                                await UpdateUser(userBySam, userDto, managerMap);
                                response.UpdatedUsers++;
                            }
                            else
                            {
                                await CreateNewUser(userDto, managerMap);
                                response.AddedUsers++;
                            }
                        }

                        response.TotalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при обработке пользователя {SamAccountName}", userDto.SamAccountName);
                        response.Errors.Add($"Ошибка обработки пользователя {userDto.SamAccountName}: {ex.Message}");
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Жесткая синхронизация завершена. Добавлено: {Added}, Обновлено: {Updated}, " +
                    "Деактивировано: {Deactivated}, Удалено: {Deleted}, Обработано: {Total}",
                    response.AddedUsers, response.UpdatedUsers, response.DeactivatedUsers,
                    response.DeletedUsers, response.TotalProcessed);

                response.Status = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при жесткой синхронизации");
                response.Status = "error";
                response.Errors.Add($"Критическая ошибка: {ex.Message}");
            }

            return response;
        }

        private async Task<User> CreateNewUser(UserToSynchronizeDto dto, Dictionary<string, Guid> managerMap)
        {
            _logger.LogInformation("Создание нового пользователя: {SamAccountName}", dto.SamAccountName);
            var user = new User
            {
                User_id = Guid.NewGuid(),
                Login = dto.Login,
                Password = "LDAP_SYNCED_USER",
                Role = AuthOptions.ROLE_USER,
                SamAccountName = dto.SamAccountName,
                Email = dto.Email,
                IsActive = dto.IsActive,
                LastAdSync = dto.LastAdSync,
                AdGuid = dto.AdGuid,
                Created_at = DateTime.UtcNow,
                Updated_at = DateTime.UtcNow
            };
            if (!string.IsNullOrEmpty(dto.ManagerAdGuid) &&
                managerMap.TryGetValue(dto.ManagerAdGuid, out var managerId))
            {
                user.Manager_id = managerId;
            }
            user.PersonalInfo = new PersonalInfo
            {
                Last_name = dto.PersonalInfo.LastName ?? string.Empty,
                First_name = dto.PersonalInfo.FirstName ?? string.Empty,
                Patronymic = dto.PersonalInfo.Patronymic ?? string.Empty,
                Birth_date = dto.PersonalInfo.BirthDate ?? DateTime.UtcNow.AddYears(-25),
                Interests = dto.PersonalInfo.Interests ?? string.Empty
            };

            user.WorkInfo = new WorkInfo
            {
                Position = dto.WorkInfo.Position ?? "Employee",
                Department = dto.WorkInfo.Department ?? "General",
                Work_exp = dto.WorkInfo.WorkExp ?? DateTime.UtcNow.AddYears(-1)
            };

            user.ContactInfo = new ContactInfo
            {
                Phone = dto.ContactInfo.Phone ?? string.Empty,
                City = dto.ContactInfo.City ?? string.Empty
            };

            await _userRepository.AddAsync(user);
            return user;
        }

        private async Task UpdateUser(User user, UserToSynchronizeDto dto, Dictionary<string, Guid> managerMap)
        {
            _logger.LogDebug("Обновление пользователя: {SamAccountName}", dto.SamAccountName);

            user.Login = dto.Login;
            user.Email = dto.Email;
            user.SamAccountName = dto.SamAccountName;
            user.IsActive = dto.IsActive;
            user.LastAdSync = dto.LastAdSync;
            user.AdGuid = dto.AdGuid;
            user.Updated_at = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(dto.ManagerAdGuid) &&
                managerMap.TryGetValue(dto.ManagerAdGuid, out var managerId))
            {
                user.Manager_id = managerId;
            }
            else
            {
                user.Manager_id = null;
            }

            if (user.PersonalInfo == null)
            {
                user.PersonalInfo = new PersonalInfo {};
            }

            user.PersonalInfo.Last_name = dto.PersonalInfo.LastName ?? string.Empty;
            user.PersonalInfo.First_name = dto.PersonalInfo.FirstName ?? string.Empty;
            user.PersonalInfo.Patronymic = dto.PersonalInfo.Patronymic ?? string.Empty;
            user.PersonalInfo.Birth_date = dto.PersonalInfo.BirthDate ?? user.PersonalInfo.Birth_date;
            user.PersonalInfo.Interests = dto.PersonalInfo.Interests ?? string.Empty;
            user.Updated_at = DateTime.UtcNow;

            // Обновляем рабочую информацию
            if (user.WorkInfo == null)
            {
                user.WorkInfo = new WorkInfo {  };
            }

            user.WorkInfo.Position = dto.WorkInfo.Position ?? "Employee";
            user.WorkInfo.Department = dto.WorkInfo.Department ?? "General";
            user.WorkInfo.Work_exp = dto.WorkInfo.WorkExp ?? user.WorkInfo.Work_exp;
            user.Updated_at = DateTime.UtcNow;

            // Обновляем контактную информацию
            if (user.ContactInfo == null)
            {
                user.ContactInfo = new ContactInfo {  };
            }

            user.ContactInfo.Phone = dto.ContactInfo.Phone ?? string.Empty;
            user.ContactInfo.City = dto.ContactInfo.City ?? string.Empty;
            user.Updated_at = DateTime.UtcNow;

            _userRepository.Update(user);
        }

    }
}
