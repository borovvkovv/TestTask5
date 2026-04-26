using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public record Employee(string Name);

// Один отпуск сотрудника.
public record VacationPeriod(DateTime Start, int LengthDays)
{
    public DateTime End => Start.AddDays(LengthDays - 1);
    public IEnumerable<DateTime> Days()
    {
        for (var d = Start.Date; d <= End.Date; d = d.AddDays(1))
            yield return d;
    }
}

public class VacationGenerator
{
    private readonly Random _rand = new Random();
    private readonly int _year;
    private readonly int[] _allowedVacationLengths;
    private readonly List<Employee> _employees;
    private readonly List<VacationPeriod> _allVacations = new();

    public VacationGenerator(int year, IEnumerable<Employee> employees, IEnumerable<int> vacationLengths)
    {
        _year = year;
        _employees = employees.ToList();
        _allowedVacationLengths = vacationLengths.ToArray();
    }

    // Сгенерировать ежегодные отпуска.
    public Dictionary<string, List<VacationPeriod>> Generate()
    {
        var result = new Dictionary<string, List<VacationPeriod>>();

        foreach (var emp in _employees)
        {
            var vacations = GenerateForEmployee();
            result[emp.Name] = vacations;
            _allVacations.AddRange(vacations);
        }

        return result;
    }

    //Сгенерировать отпуск для одного сотрудника.
    private List<VacationPeriod> GenerateForEmployee()
    {
        var minLen = _allowedVacationLengths.Min();

        var candidates = BuildCandidatePeriods();

        var bestVacationPeriods = new List<VacationPeriod>();
        Search(candidates, 0, new List<VacationPeriod>(), 0, ref bestVacationPeriods, minLen);

        return bestVacationPeriods;
    }

    // Сгенерировать всевозможные варианты отпусков согласно ограничениям.
    private List<VacationPeriod> BuildCandidatePeriods()
    {
        var periods = new List<VacationPeriod>();
        var start = new DateTime(_year, 1, 1);
        var end = new DateTime(_year, 12, 31);

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            foreach (var len in _allowedVacationLengths)
            {
                var periodEnd = date.AddDays(len);
                if (periodEnd > end)
                    continue;

                if (!ConflictsWithOthers(date, len))
                    periods.Add(new VacationPeriod(date, len));
            }
        }

        return periods
            .OrderBy(p => GetRandomDayOfYear())
            .ToList();
    }

    // Поиск наилучшего (по заданным критериям) варианта отпусков для сотрудника.
    private void Search(
        List<VacationPeriod> allVacationPlans,
        int allVacationPlansIndex,
        List<VacationPeriod> currentVacationPlan,
        int currentDays,
        ref List<VacationPeriod> bestVacationPlan,
        int minVacationLength)
    {
        if (currentDays >= 28)
        {
            var overshoot = currentDays - 28;
            if (overshoot < minVacationLength)
            {
                var minLength = Math.Min(currentVacationPlan.Count(), bestVacationPlan.Count());
                if (bestVacationPlan.Count == 0 || ScorePlan(currentVacationPlan, minLength) > ScorePlan(bestVacationPlan, minLength))
                    bestVacationPlan = currentVacationPlan.ToList();
            }
            return;
        }

        for (int i = allVacationPlansIndex; i < allVacationPlans.Count(); i++)
        {
            var cand = allVacationPlans[i];

            currentVacationPlan.Add(cand);
            _allVacations.Add(cand);

            Search(allVacationPlans, i + 1, currentVacationPlan, currentDays + cand.LengthDays, ref bestVacationPlan, minVacationLength);

            _allVacations.RemoveAt(_allVacations.Count - 1);
            currentVacationPlan.RemoveAt(currentVacationPlan.Count - 1);
        }
    }

    // Проверка на не пересечение отпуска с остальными отпусками в отделе.
    private bool ConflictsWithOthers(DateTime vacationStart, int vacationLength)
    {
        var end = vacationStart.AddDays(vacationLength);

        foreach (var v in _allVacations)
        {
            if (vacationStart <= v.End.AddDays(3) && end >= v.Start.AddDays(-3))
                return true;
        }

        return false;
    }

    // Получение рандомного числа от 0 до 365.
    private int GetRandomDayOfYear()
    {
        return _rand.Next(0, 365);
    }

    // Оценка распределенности отпусков сотрудника по кварталам.
    private int ScorePlan(List<VacationPeriod> employeeVacations, int scanFirstNVacations)
    {
        // Оценка распределения по кварталам года.
        var counts = new int[4] { 0, 0, 0, 0 };
        for (var i = 0; i < scanFirstNVacations; i++)
        {
            var quartalNo = (employeeVacations[i].Start.Month - 1) / 3;
            if (counts[quartalNo] == 1)
                counts[quartalNo] = 0;
            else
            {
                counts[quartalNo] = 1;
            }
        }

        return counts.Sum();
    }
}

public static class Program
{
    public static void Main()
    {
        var employees = new[]
        {
            new Employee("Иванов Иван Иванович"),
            new Employee("Петров Петр Петрович"),
            new Employee("Юлина Юлия Юлиановна"),
            new Employee("Сидоров Сидор Сидорович"),
            new Employee("Павлов Павел Павлович"),
            new Employee("Георгиев Георг Георгиевич")
        };

        var allowedVacationLengths = new[] { 7, 14 };

        var generator = new VacationGenerator(DateTime.Now.Year, employees, allowedVacationLengths);
        var result = generator.Generate();
        Console.OutputEncoding = Encoding.UTF8;
        foreach (var emp in result)
        {
            Console.WriteLine(emp.Key);
            foreach (var vacation in emp.Value)
            {
                Console.WriteLine($"{vacation.Start:dd.MM.yyyy}..{vacation.End:dd.MM.yyyy} ({vacation.LengthDays})");
            }
            Console.WriteLine();
        }

        Console.ReadKey();
    }
}