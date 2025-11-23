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
            new Hierarchy { HierarchyId = 1, ParentId = null, LevelHierarchy = 1, TitleHierarchy = "UDV GROUP", ColorHierarchy = "#000000" },

            new Hierarchy { HierarchyId = 2, ParentId = 1, LevelHierarchy = 2, TitleHierarchy = "UDV Digital Transformation", ColorHierarchy = "#FF5733" },
            new Hierarchy { HierarchyId = 3, ParentId = 1, LevelHierarchy = 2, TitleHierarchy = "UDV Security", ColorHierarchy = "#3498DB" },
            new Hierarchy { HierarchyId = 4, ParentId = 1, LevelHierarchy = 2, TitleHierarchy = "UDV Services", ColorHierarchy = "#2ECC71" },

            new Hierarchy { HierarchyId = 5, ParentId = 2, LevelHierarchy = 3, TitleHierarchy = "ТриниДата", ColorHierarchy = "#F39C12" },
            new Hierarchy { HierarchyId = 6, ParentId = 2, LevelHierarchy = 3, TitleHierarchy = "ВНЕ ОЧЕРЕДИ", ColorHierarchy = "#F39C12" },
            new Hierarchy { HierarchyId = 7, ParentId = 2, LevelHierarchy = 3, TitleHierarchy = "ФТ-СОФТ", ColorHierarchy = "#F39C12" },

            new Hierarchy { HierarchyId = 8, ParentId = 3, LevelHierarchy = 3, TitleHierarchy = "КИТ", ColorHierarchy = "#8E44AD" },
            new Hierarchy { HierarchyId = 9, ParentId = 3, LevelHierarchy = 3, TitleHierarchy = "КИТ.Р", ColorHierarchy = "#8E44AD" },
            new Hierarchy { HierarchyId = 10, ParentId = 3, LevelHierarchy = 3, TitleHierarchy = "Сайберлимфа", ColorHierarchy = "#8E44AD" },

            new Hierarchy { HierarchyId = 11, ParentId = 4, LevelHierarchy = 3, TitleHierarchy = "Витропс", ColorHierarchy = "#16A085" },
            // ТриниДата
            new Hierarchy { HierarchyId = 12, ParentId = 5, LevelHierarchy = 4, TitleHierarchy = "Основное подразделение", ColorHierarchy = "#7F8C8D" },
            // ВНЕ ОЧЕРЕДИ
            new Hierarchy { HierarchyId = 13, ParentId = 6, LevelHierarchy = 4, TitleHierarchy = "Основное подразделение", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 14, ParentId = 6, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктовой разработки", ColorHierarchy = "#7F8C8D" },
            // ФТ-СОФТ
            new Hierarchy { HierarchyId = 15, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 16, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктовой разработки 1", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 17, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктовой разработки 2", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 18, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Отдел заказной разработки", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 19, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Основное подразделение", ColorHierarchy = "#7F8C8D" },
            // КИТ
            new Hierarchy { HierarchyId = 20, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 21, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Департамент консалтинга", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 22, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Отдел сопровождения информационных систем", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 23, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Департамент по работе с иностранными заказчиками", ColorHierarchy = "#7F8C8D" },
            // КИТ.Р
            new Hierarchy { HierarchyId = 24, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 25, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Департамент разработки", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 26, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Департамент кибербезопасности", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 27, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Департамент маркетинга", ColorHierarchy = "#7F8C8D" },
            // Сайберлимфа
            new Hierarchy { HierarchyId = 28, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел разработки", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 29, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Коммерческий департамент", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 30, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Лаборатория кибербезопасности", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 31, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 32, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел персонала", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 33, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел технического сопровождения", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 34, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел интеграции и автоматизации", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 35, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктового менеджмента", ColorHierarchy = "#7F8C8D" },
            // Витропс
            new Hierarchy { HierarchyId = 36, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 37, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Планово-экономический отдел", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 38, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Отдел поддержки 1С", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 39, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Отдел кадрового делопроизводства", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 40, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Юридический отдел", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 41, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Служба внутреннего сервиса", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 42, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Отдел делопроизводства", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 43, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Бухгалтерия", ColorHierarchy = "#7F8C8D" },

            // ФТ-СОФТ - Основное подразделение
            new Hierarchy { HierarchyId = 44, ParentId = 19, LevelHierarchy = 5, TitleHierarchy = "Направление Аналитики и документации", ColorHierarchy = "#95A5A6" },
            // Сайберлимфа - отдел разработки
            new Hierarchy { HierarchyId = 45, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа серверной разработки", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 46, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа веб разработки", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 47, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа аналитики", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 48, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа администрирования проектов", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 49, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа продуктового дизайна", ColorHierarchy = "#7F8C8D" },
            // Сайберлимфа - Коммерческий департамент
            new Hierarchy { HierarchyId = 50, ParentId = 29, LevelHierarchy = 5, TitleHierarchy = "Отдел маркетинга", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 51, ParentId = 29, LevelHierarchy = 5, TitleHierarchy = "Отдел технической поддержки продаж", ColorHierarchy = "#7F8C8D" },
            // Сайберлимфа - Лаборатория кибербезопасности
            new Hierarchy { HierarchyId = 52, ParentId = 30, LevelHierarchy = 5, TitleHierarchy = "Производственное направление", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 53, ParentId = 30, LevelHierarchy = 5, TitleHierarchy = "Аналитическое направление", ColorHierarchy = "#7F8C8D" },
            new Hierarchy { HierarchyId = 54, ParentId = 30, LevelHierarchy = 5, TitleHierarchy = "Отдел документирования и локализации", ColorHierarchy = "#7F8C8D" },




        };

            context.Hierarchies.AddRange(hierarchy);
            context.SaveChanges();
        }
    }
}
