using System;

namespace KeyGenerationService.Dtos
{
    public class GetKeyDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public int Size { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime TakenDate { get; set; }
    }
}