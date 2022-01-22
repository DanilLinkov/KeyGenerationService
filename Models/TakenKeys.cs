using System;

namespace KeyGenerationService.Models
{
    public class TakenKey
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public int Size { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime TakenDate { get; set; }
    }
}