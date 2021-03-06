namespace MapBul.DBContext
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("mapbul.city")]
    public partial class city
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public city()
        {
            article = new HashSet<article>();
            city_permission = new HashSet<city_permission>();
            marker = new HashSet<marker>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Index(IsUnique = true)]
        public string Name { get; set; }

        public float Lng { get; set; }

        public float Lat { get; set; }

        public int CountryId { get; set; }

        [Required]
        [StringLength(100)]
        public string PlaceId { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<article> article { get; set; }

        public virtual country country { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<city_permission> city_permission { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<marker> marker { get; set; }
    }
}
