using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskFlow.Models
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string _title = "";
        private string _priority = "Средний";
        private bool _isCompleted;

        // Уникальный идентификатор задачи
        public Guid Id { get; } = Guid.NewGuid();

        // Название задачи
        public string Title
        {
            get => _title;
            set
            {
                if (_title == value)
                    return;

                _title = value;
                OnPropertyChanged();
            }
        }

        // Приоритет задачи
        public string Priority
        {
            get => _priority;
            set
            {
                if (_priority == value)
                    return;

                _priority = value;
                OnPropertyChanged();
            }
        }

        // Выполнена ли задача
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted == value)
                    return;

                _isCompleted = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}