using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using Microsoft.Data.SqlClient;
using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.Actor.ActorEMail.Tests
{
    [TestFixture]
    public class ActorEMailTests
    {
        [Test]
        public void adding_and_removing_one_mail_to_System()
        {
            var mails = TestHelper.StObjMap.StObjs.Obtain<ActorEMailTable>();
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                mails.Database.ExecuteScalar( "select PrimaryEMail from CK.vUser where UserId=1" )
                        .Should().Be( DBNull.Value );

                mails.AddEMail( ctx, 1, 1, "god@heaven.com", false );
                mails.Database.ExecuteScalar( "select PrimaryEMail from CK.vUser where UserId=1" )
                        .Should().Be( "god@heaven.com" );

                mails.RemoveEMail( ctx, 1, 1, "god@heaven.com" );
                mails.Database.ExecuteScalar( "select PrimaryEMail from CK.vUser where UserId=1" )
                        .Should().Be( DBNull.Value );
            }
        }

        [Test]
        public void first_email_is_automatically_primary_but_the_first_valid_one_is_elected()
        {
            var group = TestHelper.StObjMap.StObjs.Obtain<GroupTable>();
            var mails = TestHelper.StObjMap.StObjs.Obtain<ActorEMailTable>();
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var gId = group.CreateGroup( ctx, 1 );
                mails.AddEMail( ctx, 1, gId, "mail@address.com", false );
                mails.Database.ExecuteScalar( $"select PrimaryEMail from CK.vGroup where GroupId={gId}" )
                    .Should().Be( "mail@address.com" );

                mails.AddEMail( ctx, 1, gId, "Val-mail@address.com", false );
                mails.Database.ExecuteScalar( $"select PrimaryEMail from CK.vGroup where GroupId={gId}" )
                    .Should().Be( "mail@address.com" );

                mails.AddEMail( ctx, 1, gId, "bad-mail@address.com", false );
                mails.Database.ExecuteScalar( $"select PrimaryEMail from CK.vGroup where GroupId={gId}" )
                    .Should().Be( "mail@address.com" );


                mails.ValidateEMail( ctx, 1, gId, "Val-mail@address.com" );
                mails.Database.ExecuteScalar( $"select PrimaryEMail from CK.vGroup where GroupId={gId}" )
                    .Should().Be( "Val-mail@address.com" );

                group.DestroyGroup( ctx, 1, gId );
            }
        }

        [Test]
        public void when_removing_the_primary_email_another_one_is_elected_even_if_they_are_all_not_validated()
        {
            var user = TestHelper.StObjMap.StObjs.Obtain<UserTable>();
            var mails = TestHelper.StObjMap.StObjs.Obtain<ActorEMailTable>();
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

        [Test]
        public void EMail_unicity_can_be_dropped_if_needed()
        {
            var user = TestHelper.StObjMap.StObjs.Obtain<UserTable>();
            var mails = TestHelper.StObjMap.StObjs.Obtain<ActorEMailTable>();
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                // We need a connection that stays Opened because we are playing with begin tran/rollback accross queries.
                ctx[mails.Database].PreOpen();

                var uniqueMail = $"{Guid.NewGuid().ToString("N")}.sh@ared.com";
                var uId1 = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );
                mails.AddEMail( ctx, 1, uId1, "The-1-" + uniqueMail, isPrimary: false ).Should().Be( uId1, "The 1 is the primary mail." );
                mails.AddEMail( ctx, 1, uId1, uniqueMail, false ).Should().Be( uId1 );
                mails.Database.ExecuteScalar<string>( $"select PrimaryEMail from CK.vUser where UserId={uId1}" ) .Should().Be( "The-1-" + uniqueMail );
                mails.AddEMail( ctx, 1, uId1, uniqueMail, true ).Should().Be( uId1, "Change the primary!" );
                mails.Database.ExecuteScalar<string>( $"select PrimaryEMail from CK.vUser where UserId={uId1}" ).Should().Be( uniqueMail );
                mails.Database.ExecuteScalar<int>( $"select count(*) from CK.tActorEMail where ActorId={uId1}" ).Should().Be( 2 );

                var uId2 = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );
                mails.AddEMail( ctx, 1, uId2, "The-2-" + uniqueMail, false ).Should().Be( uId2, "The 2 is the primary mail." );
                mails.AddEMail( ctx, 1, uId2, uniqueMail, true ).Should().Be( uId1, "Another user => the first user id is returned and nothing is done." );
                // Nothing changed for both user.
                mails.Database.ExecuteScalar<string>( $"select PrimaryEMail from CK.vUser where UserId={uId1}" ).Should().Be( uniqueMail );
                mails.Database.ExecuteScalar<string>( $"select PrimaryEMail from CK.vUser where UserId={uId2}" ).Should().Be( "The-2-" + uniqueMail );
                mails.Database.ExecuteScalar<int>( $"select count(*) from CK.tActorEMail where ActorId={uId2}" ).Should().Be( 1 );

                // Calling with avoidAmbiguousEMail = false: behavior depends on UK_CK_tActorEMail_EMail constraint.
                bool isUnique = mails.Database.ExecuteScalar( "select object_id('CK.UK_CK_tActorEMail_EMail', 'UQ')" ) != DBNull.Value;
                if( isUnique )
                {
                    TestHelper.Monitor.Info( "CK.UK_CK_tActorEMail_EMail constraint found: EMail cannot be shared among users." );
                    // We cannot use the Database helpers here since the use a brand new SqlConnection each time.
                    // We must use the SqlCallContext.
                    mails.Invoking( m => m.AddEMail( ctx, 1, uId2, uniqueMail, true, avoidAmbiguousEMail:false ) ).Should().Throw<SqlDetailedException>();
                    using( Util.CreateDisposableAction( () => ctx[mails.Database].ExecuteNonQuery( new SqlCommand( "rollback;" ) ) ) )
                    {
                        ctx[mails.Database].ExecuteNonQuery( new SqlCommand( "begin tran; alter table CK.tActorEMail drop constraint UK_CK_tActorEMail_EMail;" ) );
                        TestWithoutUnicityConstraint( mails, ctx, uniqueMail, uId2 );
                    }
                }
                else
                {
                    TestHelper.Monitor.Info( "CK.UK_CK_tActorEMail_EMail constraint NOT found: EMail can be shared among users." );
                    TestWithoutUnicityConstraint( mails, ctx, uniqueMail, uId2 );
                    // We cannot test the unicity behavior here since applying the constraint will fail if current multiple emails exist.
                }

                static void TestWithoutUnicityConstraint( ActorEMailTable mails, SqlStandardCallContext ctx, string uniqueMail, int uId2 )
                {
                    mails.AddEMail( ctx, 1, uId2, uniqueMail, true, avoidAmbiguousEMail: false ).Should().Be( uId2 );
                    ctx[mails.Database].ExecuteScalar( new SqlCommand( $"select count(*) from CK.tActorEMail where ActorId={uId2}" ) ).Should().Be( 2 );
                    ctx[mails.Database].ExecuteScalar( new SqlCommand( $"select count(*) from CK.tActorEMail where EMail='{uniqueMail}'" ) ).Should().Be( 2 );
                }
            }
        }

        [Test]
        public void when_removing_the_primary_email_the_most_recently_validated_is_elected()
        {
            var user = TestHelper.StObjMap.StObjs.Obtain<UserTable>();
            var mails = TestHelper.StObjMap.StObjs.Obtain<ActorEMailTable>();
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var uId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );
                for( int i = 0; i < 10; i++ )
                {
                    mails.AddEMail( ctx, 1, uId, $"fill{i}@a.com", false, true );
                }
                mails.AddEMail( ctx, 1, uId, "2@a.com", false );
                mails.AddEMail( ctx, 1, uId, "3@a.com", true );
                System.Threading.Thread.Sleep( 100 );
                mails.AddEMail( ctx, 1, uId, "4@a.com", false, true );
                mails.AddEMail( ctx, 1, uId, "5@a.com", false );
                mails.Database.ExecuteScalar( $"select PrimaryEMail from CK.vUser where UserId={uId}" );
                mails.RemoveEMail( ctx, 1, uId, "3@a.com" );
                mails.Database.ExecuteScalar( $"select PrimaryEMail from CK.vUser where UserId={uId}" );
                user.DestroyUser( ctx, 1, uId );
            }
        }

    }
}
