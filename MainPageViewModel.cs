using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading.Tasks;
using Converter.Models;
using Converter.Services;
using Newtonsoft.Json;
using Microsoft.Maui.Storage;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Converter.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly CurrencyService _currencyService;
        private ObservableCollection<CurrencyRate> _currencyRates;
        private DateTime _selectedDate;
        private decimal _amount;
        private CurrencyRate _fromCurrency;
        private CurrencyRate _toCurrency;
        private string _result;
        private bool _isLoading;

        public MainPageViewModel()
        {
            _currencyService = new CurrencyService();
            CurrencyRates = new ObservableCollection<CurrencyRate>();

            LoadSavedData();
            LoadAvailableCurrencies();
            GetRatesCommand = new Command(async () => await GetCurrencyRates());
        }

        private async Task LoadAvailableCurrencies()
        {
            var rates = await _currencyService.GetAvailableCurrenciesAsync();
            foreach (var rate in rates)
            {
                CurrencyRates.Add(rate);
            }
        }

        public ObservableCollection<CurrencyRate> CurrencyRates
        {
            get => _currencyRates;
            set
            {
                _currencyRates = value;
                OnPropertyChanged();
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged();
                    _ = GetCurrencyRates();
                }
            }
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                if (_amount != value)
                {
                    _amount = value;
                    OnPropertyChanged();
                    CalculateConversion();
                }
            }
        }

        public CurrencyRate FromCurrency
        {
            get => _fromCurrency;
            set
            {
                if (_fromCurrency != value)
                {
                    _fromCurrency = value;
                    OnPropertyChanged();
                    CalculateConversion();
                }
            }
        }

        public CurrencyRate ToCurrency
        {
            get => _toCurrency;
            set
            {
                if (_toCurrency != value)
                {
                    _toCurrency = value;
                    OnPropertyChanged();
                    CalculateConversion();
                }
            }
        }

        public string Result
        {
            get => _result;
            set
            {
                _result = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand GetRatesCommand { get; }

        private async Task GetCurrencyRates()
        {
            IsLoading = true;
            DateTime searchDate = SelectedDate;
            bool ratesFound = false;

            while (!ratesFound)
            {
                string year = searchDate.ToString("yyyy");
                string month = searchDate.ToString("MM");
                string day = searchDate.ToString("dd");

                string url = $"https://www.cbr-xml-daily.ru/archive/{year}/{month}/{day}/daily_json.js";
                Console.WriteLine($"Fetching rates from URL: {url}");

                try
                {
                    var response = await _httpClient.GetStringAsync(url);
                    var currencyData = JsonConvert.DeserializeObject<CurrencyData>(response);

                    if (currencyData?.Valute != null && currencyData.Valute.Count > 0)
                    {
                        foreach (var rate in currencyData.Valute.Values)
                        {
                            var existingRate = CurrencyRates.FirstOrDefault(r => r.CharCode == rate.CharCode);
                            if (existingRate != null)
                            {
                                existingRate.Value = rate.Value;
                            }
                            else
                            {
                                CurrencyRates.Add(rate);
                            }
                        }

                        if (!CurrencyRates.Any(r => r.CharCode == "RUB"))
                        {
                            CurrencyRates.Add(new CurrencyRate
                            {
                                CharCode = "RUB",
                                Name = "Российский рубль",
                                Nominal = 1,
                                Value = 1
                            });
                        }

                        ratesFound = true;
                        Result = $"Курсы валют найдены на {searchDate:dd.MM.yyyy}.";
                    }
                    else
                    {
                        searchDate = searchDate.AddDays(-1);
                        Console.WriteLine($"No rates found for {searchDate:dd.MM.yyyy}, trying previous day.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching currency rates: {ex.Message}");
                    searchDate = searchDate.AddDays(-1);
                }
                if ((DateTime.Now - searchDate).TotalDays > 30)
                {
                    Result = "Не удалось найти курсы валют за последний месяц.";
                    break;
                }
            }

            IsLoading = false;

            if (!ratesFound)
            {
                Result = $"Не удалось получить курсы валют на выбранную дату и ближайшие дни. Ближайший курс найден на {searchDate:dd.MM.yyyy}.";
            }

            CalculateConversion();
        }

        private void CalculateConversion()
        {
            if (FromCurrency == null || ToCurrency == null)
            {
                Result = "Выберите валюты для конвертации.";
                return;
            }

            if (Amount <= 0)
            {
                Result = "Введите корректную сумму для конвертации.";
                return;
            }

            decimal fromValuePerNominal = FromCurrency.Value / FromCurrency.Nominal;
            decimal toValuePerNominal = ToCurrency.Value / ToCurrency.Nominal;

            if (toValuePerNominal == 0)
            {
                Result = "Ошибка: Неверный курс для конвертации.";
                return;
            }

            var convertedAmount = Amount * (fromValuePerNominal / toValuePerNominal);

            Result = $"{Amount} {FromCurrency.Name} = {convertedAmount:F2} {ToCurrency.Name}";
        }

        private void LoadSavedData()
        {
            string savedDate = Preferences.Get("SelectedDate", DateTime.Now.ToString("o"));
            SelectedDate = DateTime.Parse(savedDate);
        }

        private void SaveData()
        {
            Preferences.Set("SelectedDate", SelectedDate.ToString("o"));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(SelectedDate))
            {
                SaveData();
            }
        }
    }
}