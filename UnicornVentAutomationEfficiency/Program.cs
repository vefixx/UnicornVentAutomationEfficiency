using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using UnicornVentAutomationEfficiency.Entities;
using UnicornVentAutomationEfficiency.Models;

namespace UnicornVentAutomationEfficiency;

class Program
{
    static void Main(string[] args)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        if (!File.Exists("config.json"))
        {
            Console.WriteLine($"Конфигурационный файл config.json не найден в корневой директории скрипта");
            return;
        }


        Config? config;
        try
        {
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            if (config is null)
            {
                Console.WriteLine("Ошибка при чтении конфигурации config.json");
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка при чтении конфигурации config.json");
            return;
        }

        var connectionString = $"Data Source={config.DatabasePath}";

        string sql;
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            // Все строки из промежуточного представления view_vent_automation_efficiency
            // Отсортируем по ID квартиры, чтобы облегчить алгоритм
            sql = "SELECT * FROM view_vent_automation_efficiency ORDER BY apartment_id ASC";
            var viewRows = connection.Query<ViewRow>(sql).ToList();

            if (viewRows.Count == 0)
            {
                Console.WriteLine("Количество строк в промежуточном представление = 0. Выход");
                return;
            }

            // Алгоритм
            // 1. Отсортируем строки по возрастанию `apartment_id`
            // 2. Найдем строки, где положение `vent_on` переходит 0 -> 1 (в результате у нас должно быть две строки: первая с `vent_on = 0` (строка `A`), а вторая с `vent_on = 1` - она нам не нужна.
            // 3. Начиная со строки `A` будем идти вперед, пока не дойдем до `vent_on = 0` (то есть где `vent_on` переходит 1 -> 0) или изменения `apartment_id`. Конечную строку с `vent_on = 1` назовем строкой `B`
            // 4. В промежутке от `A` до `B` (не включая `A`) найдем минимум значения `co2_ppm` - назовем `co2_ppm_min`
            // 5. Рассчитаем "эффективность" по формуле `(A.co2_ppm - co2_ppm_min) / A.co2_ppm * 100` = `efficiency_pct` (в %)
            // 6. Добавим в витрину новую строку, где в столбце `vent_on_ts` будет записана дата и время включения вентиляции (`A.ts`), а в `vent_off_ts` будет записана дата и время выключения (`B.ts`)

            // Результаты. Строки, которые будут записаны в витрину
            List<EfficiencyRow> efficiencyRows = new List<EfficiencyRow>();

            // Строка A в алгоритме
            ViewRow? a = null;

            // Минимальный co2 в промежутке
            double co2PpmMin = double.MaxValue;

            for (int i = 0; i < viewRows.Count - 1; i++)
            {
                // Изменяем значение минимума co2
                if (a is not null && viewRows[i].VentOn)
                {
                    double currCo2 = viewRows[i].Co2Ppm;
                    co2PpmMin = currCo2 < co2PpmMin ? currCo2 : co2PpmMin;
                }

                if (!viewRows[i].VentOn && viewRows[i + 1].VentOn &&
                    viewRows[i].ApartmentId == viewRows[i + 1].ApartmentId)
                {
                    a = viewRows[i]; // Запоминаем строку A
                }
                // Переход из положения 1 в положение 0 или изменение apartment_id
                else if (a is not null && (
                             (viewRows[i].VentOn && !viewRows[i + 1].VentOn) ||
                             (viewRows[i].ApartmentId != viewRows[i + 1].ApartmentId)
                         ))
                {
                    var b = viewRows[i]; // Запоминаем строку B

                    // Проверяем переход из одного ID квартиры в другой
                    if (a.ApartmentId != b.ApartmentId)
                    {
                        // Если же ID квартиры переходит в другой, то строка b будет предыдущей
                        b = viewRows[i - 1];
                    }

                    // Рассчитываем эффективность
                    var efficiencyPct = (a.Co2Ppm - co2PpmMin) / a.Co2Ppm * 100;

                    DateTime ventOffTs = DateTime.Parse(b.Ts);
                    DateTime ventOnTs = DateTime.Parse(a.Ts);
                    var ventOnDurationH = (int)(ventOffTs - ventOnTs).TotalHours;
                    Console.WriteLine(
                        $"apartment={a.ApartmentId} | start={a.Ts}; end={b.Ts} | efficiency={efficiencyPct} | min={co2PpmMin} | durationH={ventOnDurationH}");

                    // Добавляем строку
                    efficiencyRows.Add(
                        new EfficiencyRow()
                        {
                            VentOnTs = ventOnTs.ToString("yyyy-MM-dd HH:mm:ss"),
                            VentOffTs = ventOffTs.ToString("yyyy-MM-dd HH:mm:ss"),
                            Complex = a.Complex,
                            BuildingId = a.BuildingId,
                            Building = a.Building,
                            ApartmentId = a.ApartmentId,
                            ApartmentNo = a.ApartmentNo,
                            Co2PpmMin = co2PpmMin,
                            EfficiencyPct = efficiencyPct,
                            DurationHours = ventOnDurationH
                        }
                    );

                    // Сбрасываем для следующего промежутка
                    a = null;
                    co2PpmMin = double.MaxValue;
                }
            }

            Console.WriteLine($"Количество строк в витрине: {efficiencyRows.Count}");


            // connection.Execute(
            //     """
            //     CREATE TABLE IF NOT EXISTS m_vent_automation_efficiency (
            //         vent_on_ts TEXT NOT NULL,
            //         vent_off_ts TEXT NOT NULL,
            //         building_id INTEGER NOT NULL,
            //         building TEXT NOT NULL,
            //         complex TEXT NOT NULL,
            //         apartment_id INTEGER NOT NULL,
            //         apartment_no TEXT NOT NULL,
            //         co2_ppm_min REAL NOT NULL,
            //         efficiency_pct REAL NOT NULL,
            //         FOREIGN KEY (building_id) REFERENCES building(building_id),
            //         FOREIGN KEY (apartment_id) REFERENCES apartment(apartment_id)
            //     )
            //     """
            // );
        }
    }
}