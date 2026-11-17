using System;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Newtonsoft.Json;
using TaskFlow.Models;

namespace TaskFlow
{
    public partial class MainWindow : Window
    {
        // Коллекция всех задач
        private readonly ObservableCollection<TaskItem> _tasks =
            new ObservableCollection<TaskItem>();

        // Представление коллекции для поиска и фильтрации
        private ICollectionView _tasksView;

        // Текущий фильтр
        private string _currentFilter = "Все";

        public MainWindow()
        {
            InitializeComponent();

            _tasksView = CollectionViewSource.GetDefaultView(_tasks);

            TasksListBox.ItemsSource = _tasksView;

            _tasksView.Filter = FilterTask;

            // Загружаем ранее сохранённые задачи
            LoadTasks();

            UpdateStatistics();
        }
        // Путь к файлу с сохранёнными задачами
        private readonly string _filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "tasks.json");

        // Добавление новой задачи
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleTextBox.Text.Trim();

            // Проверяем, что название введено
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    "Введите название задачи.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                TaskTitleTextBox.Focus();
                return;
            }

            // Получаем выбранный приоритет
            ComboBoxItem selectedItem =
                PriorityComboBox.SelectedItem as ComboBoxItem;

            string priority = selectedItem != null
                ? selectedItem.Content.ToString()
                : "Средний";

            // Создаём задачу
            TaskItem task = new TaskItem
            {
                Title = title,
                Priority = priority,
                IsCompleted = false
            };

            // Подписываемся на изменения свойств задачи
            task.PropertyChanged += Task_PropertyChanged;

            _tasks.Add(task);

            _tasksView.Refresh();
            UpdateStatistics();

            // Сохраняем изменения
            SaveTasks();

            // Очищаем поле ввода
            TaskTitleTextBox.Clear();
            TaskTitleTextBox.Focus();
        }

        // Изменение статуса задачи
        private void Task_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskItem.IsCompleted))
            {
                UpdateStatistics();
                _tasksView.Refresh();

                // Сохраняем новый статус
                SaveTasks();
            }
        }

        // Удаление задачи
        private void DeleteTask_Click(
            object sender,
            RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
                return;

            TaskItem task = button.Tag as TaskItem;

            if (task == null)
                return;

            MessageBoxResult result = MessageBox.Show(
                "Удалить задачу \"" + task.Title + "\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                task.PropertyChanged -= Task_PropertyChanged;

                _tasks.Remove(task);

                _tasksView.Refresh();
                UpdateStatistics();

                // Сохраняем список после удаления
                SaveTasks();
            }
            }

        // Поиск по названию
        private void SearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_tasksView != null)
                _tasksView.Refresh();
        }

        // Отображение всех задач
        private void ShowAll_Click(
            object sender,
            RoutedEventArgs e)
        {
            _currentFilter = "Все";
            _tasksView.Refresh();
        }
        // Обработчик кнопки сортировки
        private void SortByTitle_Click(
            object sender,
            RoutedEventArgs e)
        {
            SortTasksByTitle();
        }

        // Отображение активных задач
        private void ShowActive_Click(
            object sender,
            RoutedEventArgs e)
        {
            _currentFilter = "Активные";
            _tasksView.Refresh();
        }

        // Отображение выполненных задач
        private void ShowCompleted_Click(
            object sender,
            RoutedEventArgs e)
        {
            _currentFilter = "Выполненные";
            _tasksView.Refresh();
        }

        // Условие поиска и фильтрации
        private bool FilterTask(object item)
        {
            TaskItem task = item as TaskItem;

            if (task == null)
                return false;

            // Проверяем статус
            if (_currentFilter == "Активные" && task.IsCompleted)
                return false;

            if (_currentFilter == "Выполненные" && !task.IsCompleted)
                return false;

            // Проверяем поисковый запрос
            string searchText = SearchTextBox == null
                ? ""
                : SearchTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (task.Title.IndexOf(
                    searchText,
                    System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        // Обновление статистики
        private void UpdateStatistics()
        {
            int total = _tasks.Count;
            int completed = 0;

            foreach (TaskItem task in _tasks)
            {
                if (task.IsCompleted)
                    completed++;
            }

            int active = total - completed;

            TotalCountText.Text = total.ToString();
            ActiveCountText.Text = active.ToString();
            CompletedCountText.Text = completed.ToString();
        }

        // Сортировка задач по названию
        private void SortTasksByTitle()
        {
            _tasksView.SortDescriptions.Clear();

            _tasksView.SortDescriptions.Add(
                new SortDescription(
                    nameof(TaskItem.Title),
                    ListSortDirection.Ascending));
        }
        // Сохранение всех задач в JSON
        private void SaveTasks()
        {
            try
            {
                var data = _tasks.Select(task => new TaskStorageItem
                {
                    Id = task.Id,
                    Title = task.Title,
                    Priority = task.Priority,
                    IsCompleted = task.IsCompleted
                }).ToList();

                string json = JsonConvert.SerializeObject(
                    data,
                    Formatting.Indented);

                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось сохранить задачи:\n" + ex.Message,
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        // Загрузка задач из JSON
        private void LoadTasks()
        {
            if (!File.Exists(_filePath))
                return;

            try
            {
                string json = File.ReadAllText(_filePath);

                var data =
                    JsonConvert.DeserializeObject<
                        System.Collections.Generic.List<TaskStorageItem>>(json);

                if (data == null)
                    return;

                _tasks.Clear();

                foreach (TaskStorageItem item in data)
                {
                    TaskItem task = new TaskItem
                    {
                        Title = item.Title ?? "",
                        Priority = string.IsNullOrWhiteSpace(item.Priority)
                            ? "Средний"
                            : item.Priority,
                        IsCompleted = item.IsCompleted
                    };

                    task.PropertyChanged += Task_PropertyChanged;

                    _tasks.Add(task);
                }

                _tasksView.Refresh();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить задачи:\n" + ex.Message,
                    "Ошибка загрузки",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}