using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevController : ControllerBase
    {
        private readonly UserDb _context;

        public DevController(UserDb context)
        {
            _context = context;
        }

        [HttpPost("seed-users")]
        public async Task<IActionResult> SeedUsersIfEmpty()
        {
            try
            {
                var userCount = await _context.Users.AsNoTracking().CountAsync();
                if (userCount > 0)
                {
                    return Ok(new { message = $"Таблица уже заполнена: {userCount} пользователей." });
                }


                await _context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE users RESTART IDENTITY CASCADE;"
        );
                await _context.Database.ExecuteSqlRawAsync(
            @"
    INSERT INTO users (
        ""User_id"", ""Login"", ""Password"", ""Manager_id"", ""Role"", 
        ""last_name"", ""first_name"", ""patronymic"", ""birth_date"", ""interests"", 
        ""position"", ""department"", ""work_exp"", ""phone"", ""city"", 
        ""avatar"", ""new_avatar"", ""Contacts"", ""SamAccountName"", ""Email"", 
        ""IsActive"", ""LastAdSync"", ""AdGuid"", ""Created_at"", ""Updated_at"",
        ""HierarchyId""
    ) VALUES
    -- 1. Направление Аналитики и документации (44)
    ('10000000-0000-0000-0000-000000000044', 'a.smirnova', 'hashed_password_44', NULL, 'Admin', 'Смирнова', 'Анна', 'Павловна', '1988-06-12', 'аналитика, книги', 'Руководитель', 'Аналитика', '2015-01-01', '+7-495-444-44-44', 'Москва', '', '', '{\""telegram\"": \""@asmirnova\"", \""linkedin\"": \""annasmirnova\""}', 'smirnovap', 'a.smirnova@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000044', NOW(), NOW(), 44),
    ('10000000-0000-0000-0000-000000000045', 'i.petrov', 'hashed_password_45', '10000000-0000-0000-0000-000000000044', 'User', 'Петров', 'Иван', 'Андреевич', '1992-03-20', 'аналитика, спорт', 'Аналитик', 'Аналитика', '2018-01-01', '+7-495-444-44-45', 'Москва', '', '', '{\""telegram\"": \""@ipetrov_an\"", \""skype\"": \""ivan.petrov\""}', 'petrovia', 'i.petrov@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000045', NOW(), NOW(), 44),
    ('10000000-0000-0000-0000-000000000105', 't.testov', 'hashed_password_105', '10000000-0000-0000-0000-000000000045', 'User', 'Иванов', 'Петр', 'Андреевич', '1992-03-20', 'аналитика, спорт', 'Аналитик', 'Аналитика', '2018-01-01', '+7-495-444-44-45', 'Москва', '', '', '{\""telegram\"": \""@ipetrov_an\"", \""skype\"": \""ivan.petrov\""}', 'petrovia', 'i.petrov@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000105', NOW(), NOW(), 44),
    -- 2. Группа серверной разработки (45)
    ('10000000-0000-0000-0000-000000000046', 'd.kozlov', 'hashed_password_46', NULL, 'User', 'Козлов', 'Дмитрий', 'Сергеевич', '1987-11-05', 'серверы, Linux', 'Руководитель', 'Разработка', '2014-01-01', '+7-495-454-54-54', 'Москва', '', '', '{\""telegram\"": \""@dmitry_k\"", \""github\"": \""dmitrykozlov\""}', 'kozlovs', 'd.kozlov@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000046', NOW(), NOW(), 45),
    ('10000000-0000-0000-0000-000000000047', 'e.morozova', 'hashed_password_47', '10000000-0000-0000-0000-000000000046', 'User', 'Морозова', 'Елена', 'Дмитриевна', '1990-08-17', 'DevOps, автоматизация', 'Разработчик', 'Разработка', '2017-01-01', '+7-495-454-54-55', 'Москва', '', '', '{\""telegram\"": \""@emorozova\"", \""github\"": \""emoroz\""}', 'morozovadd', 'e.morozova@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000047', NOW(), NOW(), 45),

    -- 3. Группа веб разработки (46)
    ('10000000-0000-0000-0000-000000000048', 'o.fedorova', 'hashed_password_48', NULL, 'User', 'Федорова', 'Ольга', 'Игоревна', '1989-02-28', 'веб, дизайн', 'Руководитель', 'Разработка', '2016-01-01', '+7-495-464-64-64', 'Москва', '', '', '{\""telegram\"": \""@ofedorova\"", \""dribbble\"": \""olga_fed\""}', 'fedorovai', 'o.fedorova@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000048', NOW(), NOW(), 46),
    ('10000000-0000-0000-0000-000000000049', 'a.volkov', 'hashed_password_49', '10000000-0000-0000-0000-000000000048', 'User', 'Волков', 'Алексей', 'Павлович', '1993-07-10', 'фронтенд, React', 'Разработчик', 'Разработка', '2019-01-01', '+7-495-464-64-65', 'Москва', '', '', '{\""telegram\"": \""@avolkov\"", \""github\"": \""avolkov-dev\""}', 'volkovpp', 'a.volkov@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000049', NOW(), NOW(), 46),

    -- 4. Группа аналитики (47)
    ('10000000-0000-0000-0000-000000000050', 'm.sokolova', 'hashed_password_50', NULL, 'User', 'Соколова', 'Мария', 'Алексеевна', '1991-04-14', 'BI, Power BI', 'Руководитель', 'Аналитика', '2017-01-01', '+7-495-474-74-74', 'Москва', '', '', '{\""telegram\"": \""@msokolova\"", \""linkedin\"": \""mariya_sokolova\""}', 'sokolovaaa', 'm.sokolova@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000050', NOW(), NOW(), 47),
    ('10000000-0000-0000-0000-000000000051', 'n.lebedev', 'hashed_password_51', '10000000-0000-0000-0000-000000000050', 'User', 'Лебедев', 'Николай', 'Викторович', '1994-01-22', 'дата-анализ, Python', 'Аналитик', 'Аналитика', '2020-01-01', '+7-495-474-74-75', 'Москва', '', '', '{\""telegram\"": \""@nlebedev\"", \""github\"": \""nlebedev\""}', 'lebedevvv', 'n.lebedev@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000051', NOW(), NOW(), 47),

    -- 5. Группа администрирования проектов (48)
    ('10000000-0000-0000-0000-000000000052', 't.kuznetsova', 'hashed_password_52', NULL, 'User', 'Кузнецова', 'Татьяна', 'Степановна', '1986-09-03', 'управление проектами', 'Руководитель', 'Управление', '2013-01-01', '+7-495-484-84-84', 'Москва', '', '', '{\""telegram\"": \""@tkuznetsova\"", \""linkedin\"": \""tanya_kuz\""}', 'kuznetsovass', 't.kuznetsova@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000052', NOW(), NOW(), 48),
    ('10000000-0000-0000-0000-000000000053', 's.popov', 'hashed_password_53', '10000000-0000-0000-0000-000000000052', 'User', 'Попов', 'Сергей', 'Михайлович', '1995-12-08', 'Jira, Scrum', 'Менеджер проекта', 'Управление', '2021-01-01', '+7-495-484-84-85', 'Москва', '', '', '{\""telegram\"": \""@spopov\"", \""skype\"": \""sergey.popov\""}', 'popovmm', 's.popov@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000053', NOW(), NOW(), 48),

    -- 6. Группа продуктового дизайна (49)
    ('10000000-0000-0000-0000-000000000054', 'v.vasilieva', 'hashed_password_54', NULL, 'User', 'Васильева', 'Виктория', 'Романовна', '1990-05-18', 'UX/UI, Figma', 'Руководитель', 'Дизайн', '2015-01-01', '+7-495-494-94-94', 'Москва', '', '', '{\""telegram\"": \""@vvasilieva\"", \""dribbble\"": \""vika_design\""}', 'vasilievarr', 'v.vasilieva@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000054', NOW(), NOW(), 49),
    ('10000000-0000-0000-0000-000000000055', 'a.semenov', 'hashed_password_55', '10000000-0000-0000-0000-000000000054', 'User', 'Семёнов', 'Артём', 'Данилович', '1996-03-25', 'прототипирование', 'Дизайнер', 'Дизайн', '2020-01-01', '+7-495-494-94-95', 'Москва', '', '', '{\""telegram\"": \""@artem_s\"", \""figma\"": \""artem.semenov\""}', 'semenovdd', 'a.semenov@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000055', NOW(), NOW(), 49),

    -- 7. Отдел маркетинга (50)
    ('10000000-0000-0000-0000-000000000056', 'd.antonova', 'hashed_password_56', NULL, 'User', 'Антонова', 'Дарья', 'Евгеньевна', '1989-07-30', 'SMM, реклама', 'Руководитель', 'Маркетинг', '2014-01-01', '+7-495-505-05-05', 'Москва', '', '', '{\""telegram\"": \""@dasha_antonova\"", \""instagram\"": \""dasha_marketing\""}', 'antonovae', 'd.antonova@company.com', true, NOW(), 'a0000000-0000-0000-0000-000000000056', NOW(), NOW(), 50);
            "
        );
                return Ok($"добавлено, юзеров в таблице {await _context.Users.AsNoTracking().CountAsync()}");
            }
            catch(Exception e)
            {
                return BadRequest("произошла какая то ошибка: " + e);
            }
        }
    }
}
