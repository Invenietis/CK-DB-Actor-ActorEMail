using CK.DB.Actor;
using CK.DB.Actor.ActorEMail;
using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using CK.Testing;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.ActorEMail.NonUnique.Tests;


public class ActorEMailNonUniqueTests
{
    [Test]
    public void EMail_unicity_is_tested_from_CKCore_TSystem()
    {
        var mails = SharedEngine.Map.StObjs.Obtain<ActorEMailTable>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            bool isUnique = mails.Database.ExecuteScalar<bool>( "select [ActorEMailUnique] from CKCore.tSystem" );
            isUnique.Should().BeFalse();

            bool isUniqueConstraintDropped = mails.Database.ExecuteScalar( "select object_id('CK.UK_CK_tActorEMail_EMail', 'UQ')" ) == DBNull.Value;
            isUniqueConstraintDropped.Should().BeTrue();

        }
    }

    [Test]
    public void when_removing_the_primary_email_another_one_is_elected_even_if_they_are_all_not_validated()
    {
        var user = SharedEngine.Map.StObjs.Obtain<UserTable>();
        var mails = SharedEngine.Map.StObjs.Obtain<ActorEMailTable>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var uId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );
            mails.AddEMail( ctx, 1, uId, "1@a.com", false );
            mails.AddEMail( ctx, 1, uId, "2@a.com", false );
            mails.AddEMail( ctx, 1, uId, "3@a.com", true );
            mails.AddEMail( ctx, 1, uId, "4@a.com", false );
            mails.Database.ExecuteScalar( $"select PrimaryEMail from CK.vUser where UserId={uId}" )
                .Should().Be( "3@a.com" );

            mails.RemoveEMail( ctx, 1, uId, "3@a.com" );
            mails.Database.ExecuteScalar<string>( $"select PrimaryEMail from CK.vUser where UserId={uId}" )
                .Should().Match( m => m == "1@a.com" || m == "2@a.com" || m == "4@a.com" );
            user.DestroyUser( ctx, 1, uId );
        }
    }
}
