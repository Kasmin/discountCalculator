using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Homework
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var products = MakeProductsInStock();

            Console.WriteLine("Оформить заказ? да/нет (д/н): ");
            var userAnswer = Console.ReadLine();

            var isMakeOrder = userAnswer == "д" || userAnswer == "да";

            if (!isMakeOrder)
                return;

            var order = new Order();

            Console.WriteLine($"Номер вашего заказа {order.Number}");

            var isEndAddProductInOrder = false;
            while (!isEndAddProductInOrder)
            {
                Console.WriteLine($"Добавьте товар со склада в заказ или нажмите Enter, чтобы завершить");
                Console.WriteLine(GetProductNames(products));

                userAnswer = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userAnswer))
                    break;

                if (!products.ContainsKey(userAnswer))
                {
                    Console.WriteLine("При добавлении товара укажите пожалуйста букву в скобках");
                    continue;
                }

                var product = products[userAnswer];

                Console.WriteLine($"Вы выбрали {product.Name}, введите пожалуйста количество");
                userAnswer = Console.ReadLine();

                int.TryParse(userAnswer, out int productCount);

                if (order.Items.Any(x => x.Product == product))
                {
                    order.Items.First(x => x.Product == product).Count += productCount;
                    continue;
                }

                order.Items.Add(new OrderItem(product, productCount));
            }

            var isEndAddDiscount = false;
            while (!isEndAddDiscount)
            {
                Console.WriteLine("У покупателя есть скидка? Если есть укажите тип или нажмите Enter");
                Console.WriteLine(GetDiscountVariants());
                userAnswer = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userAnswer))
                    break;

                Enum.TryParse(userAnswer, out DiscountType discountType);

                var discount = DiscountFactory.Create(discountType);

                if (discount == null)
                {
                    Console.WriteLine("Не удалось распознать скидку, попробуйте еще раз");
                    continue;
                }

                order.Discount = discount;
                isEndAddDiscount = true;
            }

            Console.WriteLine("Заказ оформлен");
            Console.WriteLine();
            Console.WriteLine(order);
        }

        private static string GetProductNames(Dictionary<string, Product> products)
        {
            return string.Join(",\r\n", products.Select(x => $"({x.Key}) {x.Value.Name} - {x.Value.Price} рублей"));
        }

        private static Dictionary<string, Product> MakeProductsInStock()
        {
            return new List<Product>
            {
                new Product("Печеньки", 1000),
                new Product("Лимонад", 2000),
                new Product("Ром", 10)
            }.ToDictionary(x => x.Name.ToLower().First().ToString(), x => x);
        }

        private static string GetDiscountVariants()
        {
            var discountVariants = new Dictionary<DiscountType, string>{
                {DiscountType.GiftCard, "Подарочная карта"},
                {DiscountType.SumSale, "Сумма от стоимости"},
                {DiscountType.ProcentSale, "Процент от стоимости"},
            }.Select(x => $"({(byte)x.Key}) {x.Value}");

            return string.Join(",\r\n", discountVariants);
        }
    }

    public static class DiscountFactory
    {
        public static Discount Create(DiscountType discountType)
        {
            switch (discountType)
            {
                case DiscountType.GiftCard:
                    {
                        var giftCards = new List<GiftCard>
                        {
                            new GiftCard("1", 1000, new DateTime(2018,9,13), new DateTime(2019,9,13)),
                            new GiftCard("2", 2000, new DateTime(2017,11,1), new DateTime(2018,11,1)),
                            new GiftCard("3", 3000, new DateTime(2018,10,12), new DateTime(2019,10,12))
                        }.ToDictionary(x => x.Number, x => x);

                        Console.WriteLine("Введите номер подарочной карты:");
                        var giftCardNumber = Console.ReadLine();

                        if (!giftCards.ContainsKey(giftCardNumber))
                            return null;

                        return giftCards[giftCardNumber];
                    }
                case DiscountType.ProcentSale:
                    {
                        Console.WriteLine("Введите пожалуйста процент скидки");
                        var userAnswer = Console.ReadLine();
                        byte.TryParse(userAnswer, out byte procent);

                        return new ProcentSale(procent);
                    }
                case DiscountType.SumSale:
                    {
                        Console.WriteLine("Введите сумму скидки");
                        var userAnswer = Console.ReadLine();
                        int.TryParse(userAnswer, out int sum);

                        return new SumSale(sum);
                    }
                default:
                    return null;
            }

        }
    }

    public enum DiscountType : byte
    {
        GiftCard = 1,
        SumSale = 2,
        ProcentSale = 3
    }

    public class Order
    {
        public Order()
        {
            Number = GenerateNumber();
            Items = new List<OrderItem>();
        }

        public string Number { get; set; }
        public List<OrderItem> Items { get; set; }
        public Discount Discount { get; set; }

        private string GenerateNumber()
        {
            return $"{DateTime.Now.ToString("yyyyMMdd-HHmmss")}";
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Заказ №{Number}");
            stringBuilder.AppendLine($"Название | кол-во | стоимость |");

            foreach (var item in Items)
            {
                var productNameLength = item.Product.Name.Length;

                var productName = item.Product.Name;
                if (productNameLength < 8)
                    productName += string.Join(string.Empty, Enumerable.Range(0, 8 - productNameLength).Select(x => " "));

                stringBuilder.AppendLine("-----------------------------");
                stringBuilder.AppendLine($"{productName} |   {item.Count}    |      {item.Product.Price}");
            }

            stringBuilder.AppendLine("-----------------------------");
            var totalSum = Items.Sum(x => x.Product.Price * x.Count);
            if (Discount != null)
            {
                totalSum = Discount.Apply(totalSum);
                stringBuilder.AppendLine($"Была применена скидка: {Discount}");
                stringBuilder.AppendLine($"Итоговая стоимость со учетом скидки: {totalSum}");
            }
            else
            {
                stringBuilder.AppendLine($"Итоговая стоимость: {totalSum}");
            }

            return stringBuilder.ToString();
        }
    }

    public class OrderItem
    {
        public OrderItem(Product product, int count)
        {
            Product = product;
            Count = count;
        }

        public Product Product { get; set; }
        public int Count { get; set; }
    }

    public class Product
    {
        public Product(string name, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Продукт не может существовать без имени");

            if (price <= 0)
                throw new ArgumentException("Продукт не может быть с отрицательной или нулевой ценой");

            Name = name;
            Price = price;
        }

        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public abstract class Discount
    {
        public abstract decimal Apply(decimal price);
    }

    public class SumSale : Discount
    {
        public decimal Sum { get; private set; }

        public SumSale(decimal sum)
        {
            Sum = sum;
        }

        public override decimal Apply(decimal price)
        {
            if (price <= Sum)
                throw new ArgumentException("Итоговая сумма не может быть меньше размера скидки");

            return price - Sum;
        }

        public override string ToString()
        {
            return $"Cумма от стоимости {Sum}";
        }
    }

    public class ProcentSale : Discount
    {
        public byte Procent { get; private set; }

        public ProcentSale(byte procent)
        {
            if (Procent >= 100)
                throw new ArgumentException("Процентная скидка не может быть больше или равна 100%");

            Procent = procent;
        }

        public override decimal Apply(decimal price)
        {
            return price - price * Procent / 100;
        }

        public override string ToString()
        {
            return $"{Procent}% от стоимости";
        }
    }

    public class GiftCard : Discount
    {
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public decimal Balance { get; private set; }
        public string Number { get; private set; }

        public GiftCard(string number, decimal balance, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(number))
                throw new ArgumentException("Подарочная карта не может быть без номера");

            if (balance <= 0)
                throw new ArgumentException("Подарочная карта не может быть с нулевым или отрицательным балансом");

            if (endDate <= startDate.AddDays(1))
                throw new ArgumentException("Действие подарочной карты должно быть больше 1 дня");

            Number = number;
            Balance = balance;
            StartDate = startDate;
            EndDate = endDate;
        }

        public override decimal Apply(decimal price)
        {
            if (IsExpiration())
                throw new GiftCardExpirationException(StartDate, EndDate);

            if (price <= Balance)
            {
                Balance -= price;
                return 0;
            }

            return price - Balance;
        }

        private bool IsExpiration()
        {
            return !(StartDate <= DateTime.Now && EndDate >= DateTime.Now);
        }

        public override string ToString()
        {
            return $"Подарочная карта номиналом в {Balance} рублей";
        }
    }

    public class GiftCardExpirationException : Exception
    {
        public GiftCardExpirationException(DateTime startDate, DateTime endDate) : base($"Срок действия карты прошел, карта была действительна с {startDate} по {endDate}")
        {
        }
    }
}
