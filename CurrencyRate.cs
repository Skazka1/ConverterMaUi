namespace Converter.Models
{
    public class CurrencyRate
    {
        public string CharCode { get; set; }  // Код валюты, например, "USD"
        public decimal Value { get; set; } // Значение валюты
        public string Name { get; set; }   // Имя валюты, например, "Доллар США"
        public int Nominal { get; set; }
        public decimal ConvertedAmount { get; set; } // Добавлено свойство для хранения пересчитанной суммы
    }
}