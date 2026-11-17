using System;

namespace TaskFlow.Models
{
    // Объект, который записывается в JSON
    public class TaskStorageItem
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = "";

        public string Priority { get; set; } = "Средний";

        public bool IsCompleted { get; set; }
    }
}