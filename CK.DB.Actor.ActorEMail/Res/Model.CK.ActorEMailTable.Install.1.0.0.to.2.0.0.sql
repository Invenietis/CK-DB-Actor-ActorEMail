--[beginscript]

-- This is an enormous breaking change... Except that every known use of this package
-- de facto consider the EMail to be unique (ie. an EMail cannot be shared among users).
-- This version sets this to be the default.
alter table CK.tActorEMail add constraint UK_CK_tActorEMail_EMail unique nonclustered (EMail);

--[endscript]
