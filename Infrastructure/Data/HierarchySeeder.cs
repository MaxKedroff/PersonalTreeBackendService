using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class HierarchySeeder
    {
        public static void SeedHierarchies(UserDb context)
        {
            if (context.Hierarchies.Any()) return;

            var hierarchy = new List<Hierarchy>
        {
            new Hierarchy { HierarchyId = 1, ParentId = null, LevelHierarchy = 1, TitleHierarchy = "UDV GROUP", ColorHierarchy = "#24d07a" },

            new Hierarchy { HierarchyId = 2, ParentId = 1, LevelHierarchy = 2, TitleHierarchy = "UDV Digital Transformation", ColorHierarchy = "#7d5efa" },
            new Hierarchy { HierarchyId = 3, ParentId = 1, LevelHierarchy = 2, TitleHierarchy = "UDV Security", ColorHierarchy = "#7d5efa" },
            new Hierarchy { HierarchyId = 4, ParentId = 1, LevelHierarchy = 2, TitleHierarchy = "UDV Services", ColorHierarchy = "#7d5efa" },

            new Hierarchy { HierarchyId = 5, ParentId = 2, LevelHierarchy = 3, TitleHierarchy = "ТриниДата", ColorHierarchy = "#ff4671" },
            new Hierarchy { HierarchyId = 6, ParentId = 2, LevelHierarchy = 3, TitleHierarchy = "ВНЕ ОЧЕРЕДИ", ColorHierarchy = "#ff4671" },
            new Hierarchy { HierarchyId = 7, ParentId = 2, LevelHierarchy = 3, TitleHierarchy = "ФТ-СОФТ", ColorHierarchy = "#ff4671" },

            new Hierarchy { HierarchyId = 8, ParentId = 3, LevelHierarchy = 3, TitleHierarchy = "КИТ", ColorHierarchy = "#ff4671" },
            new Hierarchy { HierarchyId = 9, ParentId = 3, LevelHierarchy = 3, TitleHierarchy = "КИТ.Р", ColorHierarchy = "#ff4671" },
            new Hierarchy { HierarchyId = 10, ParentId = 3, LevelHierarchy = 3, TitleHierarchy = "Сайберлимфа", ColorHierarchy = "#ff4671" },

            new Hierarchy { HierarchyId = 11, ParentId = 4, LevelHierarchy = 3, TitleHierarchy = "Витропс", ColorHierarchy = "#ff4671" },
            // ТриниДата
            new Hierarchy { HierarchyId = 12, ParentId = 5, LevelHierarchy = 4, TitleHierarchy = "Основное подразделение(ТриниДата)", ColorHierarchy = "#ffab00" },
            // ВНЕ ОЧЕРЕДИ
            new Hierarchy { HierarchyId = 13, ParentId = 6, LevelHierarchy = 4, TitleHierarchy = "Основное подразделение(ВНЕ ОЧЕРЕДИ)", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 14, ParentId = 6, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктовой разработки(ВНЕ ОЧЕРЕДИ)", ColorHierarchy = "#ffab00" },
            // ФТ-СОФТ
            new Hierarchy { HierarchyId = 15, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Администрация(ФТ-СОФТ)", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 16, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктовой разработки 1", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 17, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктовой разработки 2", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 18, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Отдел заказной разработки", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 19, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Основное подразделение(ФТ-СОФТ)", ColorHierarchy = "#ffab00" },
            // КИТ
            new Hierarchy { HierarchyId = 20, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Администрация(КИТ)", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 21, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Департамент консалтинга", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 22, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Отдел сопровождения информационных систем", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 23, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Департамент по работе с иностранными заказчиками", ColorHierarchy = "#ffab00" },
            // КИТ.Р
            new Hierarchy { HierarchyId = 24, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Администрация(КИТ.Р)", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 25, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Департамент разработки", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 26, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Департамент кибербезопасности", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 27, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Департамент маркетинга", ColorHierarchy = "#ffab00" },
            // Сайберлимфа
            new Hierarchy { HierarchyId = 28, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел разработки", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 29, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Коммерческий департамент", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 30, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Лаборатория кибербезопасности", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 31, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Администрация(Сайберлимфа)", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 32, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел персонала", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 33, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел технического сопровождения", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 34, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел интеграции и автоматизации", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 35, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктового менеджмента", ColorHierarchy = "#ffab00" },
            // Витропс
            new Hierarchy { HierarchyId = 36, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Администрация(Витропс)", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 37, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Планово-экономический отдел", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 38, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Отдел поддержки 1С", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 39, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Отдел кадрового делопроизводства", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 40, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Юридический отдел", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 41, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Служба внутреннего сервиса", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 42, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Отдел делопроизводства", ColorHierarchy = "#ffab00" },
            new Hierarchy { HierarchyId = 43, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Бухгалтерия", ColorHierarchy = "#ffab00" },

            // ФТ-СОФТ - Основное подразделение
            new Hierarchy { HierarchyId = 44, ParentId = 19, LevelHierarchy = 5, TitleHierarchy = "Направление Аналитики и документации", ColorHierarchy = "#3697ff" },
            // Сайберлимфа - отдел разработки
            new Hierarchy { HierarchyId = 45, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа серверной разработки", ColorHierarchy = "#3697ff" },
            new Hierarchy { HierarchyId = 46, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа веб разработки", ColorHierarchy = "#3697ff" },
            new Hierarchy { HierarchyId = 47, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа аналитики", ColorHierarchy = "#3697ff" },
            new Hierarchy { HierarchyId = 48, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа администрирования проектов", ColorHierarchy = "#3697ff" },
            new Hierarchy { HierarchyId = 49, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа продуктового дизайна", ColorHierarchy = "#3697ff" },
            // Сайберлимфа - Коммерческий департамент
            new Hierarchy { HierarchyId = 50, ParentId = 29, LevelHierarchy = 5, TitleHierarchy = "Отдел маркетинга", ColorHierarchy = "#3697ff" },
            new Hierarchy { HierarchyId = 51, ParentId = 29, LevelHierarchy = 5, TitleHierarchy = "Отдел технической поддержки продаж", ColorHierarchy = "#3697ff" },
            // Сайберлимфа - Лаборатория кибербезопасности
            new Hierarchy { HierarchyId = 52, ParentId = 30, LevelHierarchy = 5, TitleHierarchy = "Производственное направление", ColorHierarchy = "#3697ff" },
            new Hierarchy { HierarchyId = 53, ParentId = 30, LevelHierarchy = 5, TitleHierarchy = "Аналитическое направление", ColorHierarchy = "#3697ff" },
            new Hierarchy { HierarchyId = 54, ParentId = 30, LevelHierarchy = 5, TitleHierarchy = "Отдел документирования и локализации", ColorHierarchy = "#3697ff" },




        };

            context.Hierarchies.AddRange(hierarchy);
            context.SaveChanges();
        }
    }
}
