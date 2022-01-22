using System;

namespace KeyGenerationService.Models
{
    public class AvailableKey
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public int Size { get; set; }
        public DateTime CreationDate { get; set; }
    }
}