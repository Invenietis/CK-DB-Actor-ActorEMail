using CK.Core;
using CK.DB.Actor.ActorEMail;

namespace CK.DB.Actor.ActorEMail.NonUnique
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    public abstract class Package : SqlPackage
    {
        void StObjConstruct( ActorEMail.Package package )
        {
        }
    }

    public abstract class ActorEMailNonUniqueTable : ActorEMailTable
    {
    }
}
