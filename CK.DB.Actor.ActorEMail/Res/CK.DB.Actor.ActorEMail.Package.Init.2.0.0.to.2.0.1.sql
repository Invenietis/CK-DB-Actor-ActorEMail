--[beginscript]

if COL_LENGTH('CKCore.tSystem', 'ActorEMailUnique') is null
begin
    -- Sets the behavior of the System if ActorEMail should be unique or not.
    -- This flag can be tested during migrations scripts
    alter table CKCore.tSystem add ActorEMailUnique bit not null default(1);
end

--[endscript]
