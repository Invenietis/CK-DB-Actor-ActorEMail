using CK.Core;
using CK.SqlServer;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.Actor.ActorEMail.Tests
{
    [TestFixture]
    public class ActorEMailUniqueTests
    {
        [Test]
        public void EMail_unicity_is_tested_from_CKCore_TSystem()
        {
            var mails = SharedEngine.Map.StObjs.Obtain<ActorEMailTable>();
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                bool isUnique = mails.Database.ExecuteScalar<bool>( "select [ActorEMailUnique] from CKCore.tSystem" );
                isUnique.Should().BeTrue();
            }
        }
    }
}
