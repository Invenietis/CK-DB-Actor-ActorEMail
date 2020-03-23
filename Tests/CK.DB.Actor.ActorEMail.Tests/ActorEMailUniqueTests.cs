using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Data.SqlClient;
using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.Actor.ActorEMail.Tests
{
    [TestFixture]
    public class ActorEMailUniqueTests
    {
        [Test]
        public void EMail_unicity_is_tested_from_CKCore_TSystem()
        {
            var user = TestHelper.StObjMap.StObjs.Obtain<UserTable>();
            var mails = TestHelper.StObjMap.StObjs.Obtain<ActorEMailTable>();
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                bool isUnique = mails.Database.ExecuteScalar<bool>( "select [ActorEMailUnique] from CKCore.tSystem" );
                isUnique.Should().BeTrue();
            }
        }
    }
}
