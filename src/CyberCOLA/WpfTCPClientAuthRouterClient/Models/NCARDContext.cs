using Microsoft.EntityFrameworkCore;

namespace Com.Cybersoft4u.Test.Wpf.WpfTCPClientAuthRouterClient.Models
{
    public partial class NCARDContext : DbContext
    {
        private string ConnectString;
        public NCARDContext(string connectString)
        {
            ConnectString = connectString;
        }

        public NCARDContext(DbContextOptions<NCARDContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ColaTranLog> ColaTranLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(ConnectString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ColaTranLog>(entity =>
            {
                entity.HasKey(e => new { e.TranLogKey, e.Direction, e.CreateDte });

                entity.ToTable("COLA_TRAN_LOG");

                entity.Property(e => e.TranLogKey)
                    .HasMaxLength(22)
                    .IsUnicode(false)
                    .HasColumnName("TRAN_LOG_KEY");

                entity.Property(e => e.Direction)
                    .HasMaxLength(1)
                    .IsUnicode(false)
                    .HasColumnName("DIRECTION");

                entity.Property(e => e.CreateDte).HasColumnName("CREATE_DTE");

                entity.Property(e => e.BitMap)
                    .IsRequired()
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("BIT_MAP");

                entity.Property(e => e.MessageHeader)
                    .IsRequired()
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("MESSAGE_HEADER");

                entity.Property(e => e.MessageType)
                    .IsRequired()
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasColumnName("MESSAGE_TYPE");

                entity.Property(e => e.P001)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("P_001");

                entity.Property(e => e.P002)
                    .HasMaxLength(19)
                    .IsUnicode(false)
                    .HasColumnName("P_002");

                entity.Property(e => e.P003)
                    .HasMaxLength(6)
                    .IsUnicode(false)
                    .HasColumnName("P_003");

                entity.Property(e => e.P004)
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("P_004");

                entity.Property(e => e.P005)
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("P_005");

                entity.Property(e => e.P006)
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("P_006");

                entity.Property(e => e.P007)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("P_007");

                entity.Property(e => e.P008)
                    .HasMaxLength(8)
                    .IsUnicode(false)
                    .HasColumnName("P_008");

                entity.Property(e => e.P009)
                    .HasMaxLength(8)
                    .IsUnicode(false)
                    .HasColumnName("P_009");

                entity.Property(e => e.P010)
                    .HasMaxLength(8)
                    .IsUnicode(false)
                    .HasColumnName("P_010");

                entity.Property(e => e.P011)
                    .HasMaxLength(6)
                    .IsUnicode(false)
                    .HasColumnName("P_011");

                entity.Property(e => e.P012)
                    .HasMaxLength(6)
                    .IsUnicode(false)
                    .HasColumnName("P_012");

                entity.Property(e => e.P013)
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasColumnName("P_013");

                entity.Property(e => e.P014)
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasColumnName("P_014");

                entity.Property(e => e.P015)
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasColumnName("P_015");

                entity.Property(e => e.P016)
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasColumnName("P_016");

                entity.Property(e => e.P017)
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasColumnName("P_017");

                entity.Property(e => e.P018)
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasColumnName("P_018");

                entity.Property(e => e.P019)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_019");

                entity.Property(e => e.P020)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_020");

                entity.Property(e => e.P021)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_021");

                entity.Property(e => e.P022)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_022");

                entity.Property(e => e.P023)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_023");

                entity.Property(e => e.P024)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_024");

                entity.Property(e => e.P025)
                    .HasMaxLength(2)
                    .IsUnicode(false)
                    .HasColumnName("P_025");

                entity.Property(e => e.P026)
                    .HasMaxLength(2)
                    .IsUnicode(false)
                    .HasColumnName("P_026");

                entity.Property(e => e.P027)
                    .HasMaxLength(1)
                    .IsUnicode(false)
                    .HasColumnName("P_027");

                entity.Property(e => e.P028)
                    .HasMaxLength(9)
                    .IsUnicode(false)
                    .HasColumnName("P_028");

                entity.Property(e => e.P029)
                    .HasMaxLength(8)
                    .IsUnicode(false)
                    .HasColumnName("P_029");

                entity.Property(e => e.P030)
                    .HasMaxLength(8)
                    .IsUnicode(false)
                    .HasColumnName("P_030");

                entity.Property(e => e.P031)
                    .HasMaxLength(8)
                    .IsUnicode(false)
                    .HasColumnName("P_031");

                entity.Property(e => e.P032)
                    .HasMaxLength(13)
                    .IsUnicode(false)
                    .HasColumnName("P_032");

                entity.Property(e => e.P033)
                    .HasMaxLength(13)
                    .IsUnicode(false)
                    .HasColumnName("P_033");

                entity.Property(e => e.P034)
                    .HasMaxLength(28)
                    .IsUnicode(false)
                    .HasColumnName("P_034");

                entity.Property(e => e.P035)
                    .HasMaxLength(39)
                    .IsUnicode(false)
                    .HasColumnName("P_035");

                entity.Property(e => e.P036)
                    .HasMaxLength(107)
                    .IsUnicode(false)
                    .HasColumnName("P_036");

                entity.Property(e => e.P037)
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("P_037");

                entity.Property(e => e.P038)
                    .HasMaxLength(6)
                    .IsUnicode(false)
                    .HasColumnName("P_038");

                entity.Property(e => e.P039)
                    .HasMaxLength(2)
                    .IsUnicode(false)
                    .HasColumnName("P_039");

                entity.Property(e => e.P040)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_040");

                entity.Property(e => e.P041)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("P_041");

                entity.Property(e => e.P042)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("P_042");

                entity.Property(e => e.P043)
                    .HasMaxLength(40)
                    .IsUnicode(false)
                    .HasColumnName("P_043");

                entity.Property(e => e.P044)
                    .HasMaxLength(27)
                    .IsUnicode(false)
                    .HasColumnName("P_044");

                entity.Property(e => e.P045)
                    .HasMaxLength(78)
                    .IsUnicode(false)
                    .HasColumnName("P_045");

                entity.Property(e => e.P046)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("P_046");

                entity.Property(e => e.P047)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("P_047");

                entity.Property(e => e.P048)
                    .HasMaxLength(79)
                    .IsUnicode(false)
                    .HasColumnName("P_048");

                entity.Property(e => e.P049)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_049");

                entity.Property(e => e.P050)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_050");

                entity.Property(e => e.P051)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("P_051");

                entity.Property(e => e.P052)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("P_052");

                entity.Property(e => e.P053)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("P_053");

                entity.Property(e => e.P054)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("P_054");

                entity.Property(e => e.P055)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("P_055");

                entity.Property(e => e.P056)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("P_056");

                entity.Property(e => e.P057)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("P_057");

                entity.Property(e => e.P058)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("P_058");

                entity.Property(e => e.P059)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("P_059");

                entity.Property(e => e.P060)
                    .HasMaxLength(61)
                    .IsUnicode(false)
                    .HasColumnName("P_060");

                entity.Property(e => e.P061)
                    .HasMaxLength(22)
                    .IsUnicode(false)
                    .HasColumnName("P_061");

                entity.Property(e => e.P062)
                    .HasMaxLength(13)
                    .IsUnicode(false)
                    .HasColumnName("P_062");

                entity.Property(e => e.P063)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("P_063");

                entity.Property(e => e.P064)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("P_064");

                entity.Property(e => e.S065)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("S_065");

                entity.Property(e => e.S066)
                    .HasMaxLength(1)
                    .IsUnicode(false)
                    .HasColumnName("S_066");

                entity.Property(e => e.S067)
                    .HasMaxLength(2)
                    .IsUnicode(false)
                    .HasColumnName("S_067");

                entity.Property(e => e.S068)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("S_068");

                entity.Property(e => e.S069)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("S_069");

                entity.Property(e => e.S070)
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("S_070");

                entity.Property(e => e.S071)
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasColumnName("S_071");

                entity.Property(e => e.S072)
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasColumnName("S_072");

                entity.Property(e => e.S073)
                    .HasMaxLength(6)
                    .IsUnicode(false)
                    .HasColumnName("S_073");

                entity.Property(e => e.S074)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("S_074");

                entity.Property(e => e.S075)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("S_075");

                entity.Property(e => e.S076)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("S_076");

                entity.Property(e => e.S077)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("S_077");

                entity.Property(e => e.S078)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("S_078");

                entity.Property(e => e.S079)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("S_079");

                entity.Property(e => e.S080)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("S_080");

                entity.Property(e => e.S081)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("S_081");

                entity.Property(e => e.S082)
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("S_082");

                entity.Property(e => e.S083)
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("S_083");

                entity.Property(e => e.S084)
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("S_084");

                entity.Property(e => e.S085)
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasColumnName("S_085");

                entity.Property(e => e.S086)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("S_086");

                entity.Property(e => e.S087)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("S_087");

                entity.Property(e => e.S088)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("S_088");

                entity.Property(e => e.S089)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("S_089");

                entity.Property(e => e.S090)
                    .HasMaxLength(42)
                    .IsUnicode(false)
                    .HasColumnName("S_090");

                entity.Property(e => e.S091)
                    .HasMaxLength(1)
                    .IsUnicode(false)
                    .HasColumnName("S_091");

                entity.Property(e => e.S092)
                    .HasMaxLength(2)
                    .IsUnicode(false)
                    .HasColumnName("S_092");

                entity.Property(e => e.S093)
                    .HasMaxLength(5)
                    .IsUnicode(false)
                    .HasColumnName("S_093");

                entity.Property(e => e.S094)
                    .HasMaxLength(7)
                    .IsUnicode(false)
                    .HasColumnName("S_094");

                entity.Property(e => e.S095)
                    .HasMaxLength(42)
                    .IsUnicode(false)
                    .HasColumnName("S_095");

                entity.Property(e => e.S096)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("S_096");

                entity.Property(e => e.S097)
                    .HasMaxLength(17)
                    .IsUnicode(false)
                    .HasColumnName("S_097");

                entity.Property(e => e.S098)
                    .HasMaxLength(25)
                    .IsUnicode(false)
                    .HasColumnName("S_098");

                entity.Property(e => e.S099)
                    .HasMaxLength(13)
                    .IsUnicode(false)
                    .HasColumnName("S_099");

                entity.Property(e => e.S100)
                    .HasMaxLength(13)
                    .IsUnicode(false)
                    .HasColumnName("S_100");

                entity.Property(e => e.S101)
                    .HasMaxLength(19)
                    .IsUnicode(false)
                    .HasColumnName("S_101");

                entity.Property(e => e.S102)
                    .HasMaxLength(28)
                    .IsUnicode(false)
                    .HasColumnName("S_102");

                entity.Property(e => e.S103)
                    .HasMaxLength(28)
                    .IsUnicode(false)
                    .HasColumnName("S_103");

                entity.Property(e => e.S104)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_104");

                entity.Property(e => e.S105)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_105");

                entity.Property(e => e.S106)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_106");

                entity.Property(e => e.S107)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_107");

                entity.Property(e => e.S108)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_108");

                entity.Property(e => e.S109)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_109");

                entity.Property(e => e.S110)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_110");

                entity.Property(e => e.S111)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_111");

                entity.Property(e => e.S112)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_112");

                entity.Property(e => e.S113)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_113");

                entity.Property(e => e.S114)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_114");

                entity.Property(e => e.S115)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_115");

                entity.Property(e => e.S116)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_116");

                entity.Property(e => e.S117)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_117");

                entity.Property(e => e.S118)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_118");

                entity.Property(e => e.S119)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_119");

                entity.Property(e => e.S120)
                    .HasMaxLength(114)
                    .IsUnicode(false)
                    .HasColumnName("S_120");

                entity.Property(e => e.S121)
                    .HasMaxLength(63)
                    .IsUnicode(false)
                    .HasColumnName("S_121");

                entity.Property(e => e.S122)
                    .HasMaxLength(104)
                    .IsUnicode(false)
                    .HasColumnName("S_122");

                entity.Property(e => e.S123)
                    .HasMaxLength(153)
                    .IsUnicode(false)
                    .HasColumnName("S_123");

                entity.Property(e => e.S124)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_124");

                entity.Property(e => e.S125)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("S_125");

                entity.Property(e => e.S126)
                    .HasMaxLength(999)
                    .IsUnicode(false)
                    .HasColumnName("S_126");

                entity.Property(e => e.S127)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("S_127");

                entity.Property(e => e.S128)
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("S_128");

                entity.Property(e => e.SessionName)
                    .IsRequired()
                    .HasMaxLength(80)
                    .IsUnicode(false)
                    .HasColumnName("SESSION_NAME");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
