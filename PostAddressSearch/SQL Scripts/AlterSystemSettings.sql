-- SystemSettings.GisPaToken
if not exists (select 1 from sys.objects so 
			   join sys.columns sc 
			   on so.object_id = sc.object_id
			   where so.name = 'SystemSettings' 
			   and sc.name = 'GisPaToken')
begin
	alter table SystemSettings
	add GisPaToken varchar(128) null
end
go

--SystemSettings.GisPaMainUrl
if not exists (select 1 from sys.objects so 
			   join sys.columns sc 
			   on so.object_id = sc.object_id
			   where so.name = 'SystemSettings' 
			   and sc.name = 'GisPaMainUrl')
begin
	alter table SystemSettings
	add GisPaMainUrl varchar(512) null
end
go

--SystemSettings.GisPaChildrenUrl
if not exists (select 1 from sys.objects so 
			   join sys.columns sc 
			   on so.object_id = sc.object_id
			   where so.name = 'SystemSettings' 
			   and sc.name = 'GisPaChildrenUrl')
begin
	alter table SystemSettings
	add GisPaChildrenUrl varchar(512) null
end
go

update SystemSettings
set GisPaToken = '4319be2a-2253-47b3-8944-0b69c7134d36'

update SystemSettings
set GisPaMainUrl = 'https://address.pochta.ru/suggest/api/v4_5'

update SystemSettings
set GisPaChildrenUrl = 'https://address.pochta.ru/suggest/api/v4_5/children'