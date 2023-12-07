using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Humidity
{
    public class HumidityData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int sensorId { get; set; } 
        public int humidity { get; set; }
        public DateTime dateTime { get; set; }

    }

    public class HumidityDb : DbContext
    {
        public HumidityDb(DbContextOptions<HumidityDb> options) : base(options) { }
        public DbSet<HumidityData> HumidityDatas { get; set; } = null!;
    }
}

