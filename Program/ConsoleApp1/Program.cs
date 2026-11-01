using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeliveryApp
{

    public class InvalidDeliveryParameterException : Exception
    {
        public InvalidDeliveryParameterException() { }
        public InvalidDeliveryParameterException(string message) : base(message) { }
        public InvalidDeliveryParameterException(string message, Exception inner) : base(message, inner) { }
    }

    public abstract class DeliveryMethod : IComparable<DeliveryMethod>
    {
        private string _name = string.Empty;
        private double _ratePerKg;

        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Назва способу доставки не може бути порожньою.");
                }
                _name = value.Trim();
            }
        }

        public double RatePerKg
        {
            get => _ratePerKg;
            set
            {
                if (!(value > 0))
                {
                    throw new InvalidDeliveryParameterException("Ставка за кг повинна бути більшою за нуль.");
                }
                _ratePerKg = value;
            }
        }

        protected DeliveryMethod(string name, double ratePerKg)
        {
            Name = name;
            RatePerKg = ratePerKg;
        }

        public abstract string TypeName { get; }


        public abstract double CalculateCost(double weightKg);


        public double ReferenceCost => CalculateCost(1);

        protected static void ValidateWeight(double weightKg)
        {
            if (!(weightKg >= 0))
            {
                throw new InvalidDeliveryParameterException("Вага не може бути від'ємною.");
            }
        }

        public virtual string GetDescription()
        {
            return $"{TypeName} | {Name} | ставка: {Money(RatePerKg)}/кг";
        }

        public int CompareTo(DeliveryMethod? other)
        {
            if (other == null) return 1;
            return ReferenceCost.CompareTo(other.ReferenceCost);
        }

        public static string Money(double value)
        {
            return value.ToString("F2", CultureInfo.InvariantCulture) + " грн";
        }
    }

    public class StandardDelivery : DeliveryMethod
    {
        public StandardDelivery(string name, double ratePerKg) : base(name, ratePerKg) { }

        public override string TypeName => "Стандартна";

        public override double CalculateCost(double weightKg)
        {
            ValidateWeight(weightKg);
            return weightKg * RatePerKg;
        }
    }

    public class ExpressDelivery : DeliveryMethod
    {
        private double _speedMultiplier;
        private double _surcharge;

        public double SpeedMultiplier
        {
            get => _speedMultiplier;
            set
            {
                if (!(value >= 1))
                {
                    throw new InvalidDeliveryParameterException("Множник швидкості повинен бути не меншим за 1.");
                }
                _speedMultiplier = value;
            }
        }

        public double Surcharge
        {
            get => _surcharge;
            set
            {
                if (!(value >= 0))
                {
                    throw new InvalidDeliveryParameterException("Надбавка не може бути від'ємною.");
                }
                _surcharge = value;
            }
        }

        public ExpressDelivery(string name, double ratePerKg, double speedMultiplier, double surcharge)
            : base(name, ratePerKg)
        {
            SpeedMultiplier = speedMultiplier;
            Surcharge = surcharge;
        }

        public override string TypeName => "Експрес";

        public override double CalculateCost(double weightKg)
        {
            ValidateWeight(weightKg);
            return weightKg * RatePerKg * SpeedMultiplier + Surcharge;
        }

        public override string GetDescription()
        {
            return base.GetDescription()
                   + $" | множник: {SpeedMultiplier.ToString("F2", CultureInfo.InvariantCulture)}"
                   + $" | надбавка: {Money(Surcharge)}";
        }
    }

    internal class Program
    {
        private static readonly List<DeliveryMethod> Methods = new();

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            SeedData();

            bool isRunning = true;

            while (isRunning)
            {
                PrintMenu();
                int choice = ReadInt("Ваш вибір: ", 0, 4);

                try
                {
                    isRunning = choice switch
                    {
                        1 => AddMethod(),
                        2 => CalculateDeliveryCost(),
                        3 => ShowMethods(),
                        4 => SortMethods(),
                        0 => false,
                        _ => true
                    };
                }
                catch (InvalidDeliveryParameterException ex)
                {
                    PrintError($"Некоректний параметр: {ex.Message}");
                }
                catch (ArgumentException ex)
                {
                    PrintError($"Некоректні дані: {ex.Message}");
                }
                catch (Exception ex)
                {
                    PrintError($"Непередбачена помилка: {ex.Message}");
                }
                finally
                {
                    if (choice != 0)
                    {
                        Console.WriteLine("--- Повернення до головного меню ---");
                    }
                }
            }

            Console.WriteLine("\nПрограму завершено. Дякуємо за використання!");
        }

        static void SeedData()
        {
            Methods.Add(new StandardDelivery("Стандартна доставка", 20));
            Methods.Add(new ExpressDelivery("Експрес-доставка", 35, 1.5, 50));
            Methods.Add(new StandardDelivery("Економ", 12));
        }

        static void PrintMenu()
        {
            Console.WriteLine("\n=============== СПОСОБИ ДОСТАВКИ ===============");
            Console.WriteLine("1. Додати спосіб доставки");
            Console.WriteLine("2. Розрахувати вартість доставки");
            Console.WriteLine("3. Показати довідник способів доставки");
            Console.WriteLine("4. Відсортувати способи від найдешевшого до найдорожчого");
            Console.WriteLine("0. Вихід");
            Console.WriteLine("================================================");
        }

        static bool AddMethod()
        {
            Console.WriteLine("\n--- Додавання способу доставки ---");
            int type = ReadInt("Тип (1 – стандартна, 2 – експрес): ", 1, 2);

            Console.Write("Назва: ");
            string name = Console.ReadLine() ?? string.Empty;

            double rate = ReadDouble("Ставка за кг (грн): ");

            DeliveryMethod method;

            if (type == 1)
            {
                method = new StandardDelivery(name, rate);
            }
            else
            {
                double multiplier = ReadDouble("Множник швидкості (не менше 1): ");
                double surcharge = ReadDouble("Надбавка (грн): ");
                method = new ExpressDelivery(name, rate, multiplier, surcharge);
            }

            Methods.Add(method);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Додано: {method.GetDescription()}");
            Console.ResetColor();

            return true;
        }

        static bool CalculateDeliveryCost()
        {
            Console.WriteLine("\n--- Розрахунок вартості доставки ---");

            if (Methods.Count == 0)
            {
                Console.WriteLine("Довідник порожній. Спочатку додайте спосіб доставки.");
                return true;
            }

            for (int i = 0; i < Methods.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {Methods[i].Name} ({Methods[i].TypeName})");
            }

            int index = ReadInt("Оберіть спосіб: ", 1, Methods.Count);
            double weight = ReadDouble("Вага посилки (кг): ");

            DeliveryMethod method = Methods[index - 1];
            double cost = method.CalculateCost(weight);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Результат: {method.Name}, {weight.ToString("F2", CultureInfo.InvariantCulture)} кг = {DeliveryMethod.Money(cost)}");
            Console.ResetColor();

            return true;
        }

        static bool ShowMethods()
        {
            Console.WriteLine("\n--- Довідник способів доставки ---");

            if (Methods.Count == 0)
            {
                Console.WriteLine("Довідник порожній.");
                return true;
            }

            int number = 1;
            foreach (DeliveryMethod method in Methods)
            {
                Console.WriteLine($"{number}. {method.GetDescription()} | за 1 кг: {DeliveryMethod.Money(method.ReferenceCost)}");
                number++;
            }

            return true;
        }

        static bool SortMethods()
        {
            if (Methods.Count == 0)
            {
                Console.WriteLine("\nДовідник порожній, сортувати нічого.");
                return true;
            }

            Methods.Sort();

            Console.WriteLine("\nВідсортовано від найдешевшого до найдорожчого (за вартістю 1 кг):");
            return ShowMethods();
        }

        static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Помилка] {message}");
            Console.ResetColor();
        }

        static int ReadInt(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int value) && value >= min && value <= max)
                {
                    return value;
                }

                Console.WriteLine($"Некоректне значення. Введіть ціле число від {min} до {max}.");
            }
        }

        static double ReadDouble(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (input != null)
                {
                    input = input.Trim().Replace(',', '.');

                    if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                        && double.IsFinite(value))
                    {
                        return value;
                    }
                }

                Console.WriteLine("Некоректний ввід. Будь ласка, введіть дійсне число.");
            }
        }
    }
}
