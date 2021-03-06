namespace MapBul.DBContext
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("mapbul.article")]
    public partial class article
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public article()
        {
            articlesubcategory = new HashSet<articlesubcategory>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(60)]
        public string Title { get; set; }
        
        [StringLength(60)]
        public string TitleEn { get; set; }

        [StringLength(200)]
        public string TitlePhoto { get; set; }

        [StringLength(200)]
        public string TitlePhotoPreview { get; set; }

        [StringLength(200)]
        public string Photo { get; set; }

        [Column(TypeName = "text")]
        [StringLength(65535)]
        public string SourceUrl { get; set; }

        [Column(TypeName = "text")]
        [StringLength(65535)]
        public string SourceUrlEn { get; set; }

        [Column(TypeName = "text")]
        [StringLength(65535)]
        public string SourcePhoto { get; set; }

        [Column(TypeName = "text")]
        [StringLength(65535)]
        public string SourcePhotoEn { get; set; }

        [Required]
        [StringLength(150)]
        public string Description { get; set; }
        
        [StringLength(150)]
        public string DescriptionEn { get; set; }

        [Column(TypeName = "text")]
        [Required]
        [StringLength(65535)]
        public string Text { get; set; }

        [Column(TypeName = "text")]
        [StringLength(65535)]
        public string TextEn { get; set; }

        public int AuthorId { get; set; }

        public int? EditorId { get; set; }

        public DateTime AddedDate { get; set; }

        public DateTime? PublishedDate { get; set; }

        public int? MarkerId { get; set; }

        public DateTime? StartDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public int StatusId { get; set; }

        public int BaseCategoryId { get; set; }

        public DateTime? EndDate { get; set; }

        public int? CityId { get; set; }

        public virtual user user { get; set; }

        public virtual category category { get; set; }

        public virtual city city { get; set; }

        public virtual user user1 { get; set; }

        public virtual marker marker { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<articlesubcategory> articlesubcategory { get; set; }

        public virtual status status { get; set; }
    }
}
