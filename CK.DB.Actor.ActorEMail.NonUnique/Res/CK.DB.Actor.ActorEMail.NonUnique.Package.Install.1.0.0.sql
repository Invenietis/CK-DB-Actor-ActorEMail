--[beginscript]

if  object_id('CK.UK_CK_tActorEMail_EMail', 'UQ') is not null
begin
    alter table CK.tActorEMail drop constraint UK_CK_tActorEMail_EMail;
end

--[endscript]
