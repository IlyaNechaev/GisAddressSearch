IF NOT EXISTS (SELECT 1 FROM sys.tables st
               WHERE st.[name] = 'RegionCode')
BEGIN
CREATE TABLE RegionCode
(
	ObjectId UNIQUEIDENTIFIER NOT NULL DEFAULT(NEWID()),
	[Name] NVARCHAR(127) NOT NULL,
	[Code] TINYINT NOT NULL,
	[FiasId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT PK_RegionCode PRIMARY KEY(ObjectId)
)

ALTER SCHEMA [dict]
TRANSFER [dbo].[RegionCode]

INSERT INTO [dict].[RegionCode] ([Code], [Name], [FiasId])
SELECT 14,N'Республика Саха (Якутия)', 'c225d3db-1db6-4063-ace0-b3fe9ea3805f' UNION ALL
SELECT 30,N'Астраханская область', '83009239-25cb-4561-af8e-7ee111b1cb73' UNION ALL
SELECT 28,N'Амурская область', '844a80d6-5e31-4017-b422-4d9c01e9942c' UNION ALL
SELECT 10,N'Республика Карелия', '248d8071-06e1-425e-a1cf-d1ff4c4a14a8' UNION ALL
SELECT 74,N'Челябинская область', '27eb7c10-a234-44da-a59c-8b1f864966de' UNION ALL
SELECT 08,N'Республика Калмыкия', '491cde9d-9d76-4591-ab46-ea93c079e686' UNION ALL
SELECT 39,N'Калининградская область', '90c7181e-724f-41b3-b6c6-bd3ec7ae3f30' UNION ALL
SELECT 12,N'Республика Марий Эл', 'de2cbfdf-9662-44a4-a4a4-8ad237ae4a3e' UNION ALL
SELECT 53,N'Новгородская область', 'e5a84b81-8ea1-49e3-b3c4-0528651be129' UNION ALL
SELECT 60,N'Псковская область', 'f6e148a1-c9d0-4141-a608-93e3bd95e6c4' UNION ALL
SELECT 51,N'Мурманская область', '1c727518-c96a-4f34-9ae6-fd510da3be03' UNION ALL
SELECT 36,N'Воронежская область', 'b756fe6b-bbd3-44d5-9302-5bfcc740f46e' UNION ALL
SELECT 48,N'Липецкая область', '1490490e-49c5-421c-9572-5673ba5d80c8' UNION ALL
SELECT 61,N'Ростовская область', 'f10763dc-63e3-48db-83e1-9c566fe3092b' UNION ALL
SELECT 32,N'Брянская область', 'f5807226-8be0-4ea8-91fc-39d053aec1e2' UNION ALL
SELECT 70,N'Томская область', '889b1f3a-98aa-40fc-9d3d-0f41192758ab' UNION ALL
SELECT 77,N'г. Москва', '0c5b2444-70a0-4932-980c-b4dc0d3f02b5' UNION ALL
SELECT 46,N'Курская область', 'ee594d5e-30a9-40dc-b9f2-0add1be44ba1' UNION ALL
SELECT 02,N'Республика Башкортостан', '6f2cbfd8-692a-4ee4-9b16-067210bde3fc' UNION ALL
SELECT 78,N'г. Санкт-Петербург', 'c2deb16a-0330-4f05-821f-1d09c93331e6' UNION ALL
SELECT 38,N'Иркутская область', '6466c988-7ce3-45e5-8b97-90ae16cb1249' UNION ALL
SELECT 83,N'Ненецкий автономный округ (Архангельская область)', '89db3198-6803-4106-9463-cbf781eff0b8' UNION ALL
SELECT 62,N'Рязанская область', '963073ee-4dfc-48bd-9a70-d2dfc6bd1f31' UNION ALL
SELECT 15,N'Республика Северная Осетия-Алания', 'de459e9c-2933-4923-83d1-9c64cfd7a817' UNION ALL
SELECT 71,N'Тульская область', 'd028ec4f-f6da-4843-ada6-b68b3e0efa3d' UNION ALL
SELECT 20,N'Чеченская Республика', 'de67dc49-b9ba-48a3-a4cc-c2ebfeca6c5e' UNION ALL
SELECT 56,N'Оренбургская область', '8bcec9d6-05bc-4e53-b45c-ba0c6f3a5c44' UNION ALL
SELECT 04,N'Республика Алтай', '5c48611f-5de6-4771-9695-7e36a4e7529d' UNION ALL
SELECT 37,N'Ивановская область', '0824434f-4098-4467-af72-d4f702fed335' UNION ALL
SELECT 55,N'Омская область', '05426864-466d-41a3-82c4-11e61cdc98ce' UNION ALL
SELECT 41,N'Камчатский край', 'd02f30fc-83bf-4c0f-ac2b-5729a866a207' UNION ALL
SELECT 16,N'Республика Татарстан (Татарстан)', '0c089b04-099e-4e0e-955a-6bf1ce525f1a' UNION ALL
SELECT 05,N'Республика Дагестан', '0bb7fa19-736d-49cf-ad0e-9774c4dae09b' UNION ALL
SELECT 63,N'Самарская область', 'df3d7359-afa9-4aaa-8ff9-197e73906b1c' UNION ALL
SELECT 35,N'Вологодская область', 'ed36085a-b2f5-454f-b9a9-1c9a678ee618' UNION ALL
SELECT 69,N'Тверская область', '61723327-1c20-42fe-8dfa-402638d9b396' UNION ALL
SELECT 18,N'Удмуртская Республика', '52618b9c-bcbb-47e7-8957-95c63f0b17cc' UNION ALL
SELECT 87,N'Чукотский автономный округ', 'f136159b-404a-4f1f-8d8d-d169e1374d5c' UNION ALL
SELECT 57,N'Орловская область', '5e465691-de23-4c4e-9f46-f35a125b5970' UNION ALL
SELECT 68,N'Тамбовская область', 'a9a71961-9363-44ba-91b5-ddf0463aebc2' UNION ALL
SELECT 19,N'Республика Хакасия', '8d3f1d35-f0f4-41b5-b5b7-e7cadf3e7bd7' UNION ALL
SELECT 58,N'Пензенская область', 'c99e7924-0428-4107-a302-4fd7c0cca3ff' UNION ALL
SELECT 22,N'Алтайский край', '8276c6a1-1a86-4f0d-8920-aba34d4cc34a' UNION ALL
SELECT 52,N'Нижегородская область', '88cd27e2-6a8a-4421-9718-719a28a0a088' UNION ALL
SELECT 75,N'Забайкальский край', 'b6ba5716-eb48-401b-8443-b197c9578734' UNION ALL
SELECT 72,N'Тюменская область', '54049357-326d-4b8f-b224-3c6dc25d6dd3' UNION ALL
SELECT 31,N'Белгородская область', '639efe9d-3fc8-4438-8e70-ec4f2321f2a7' UNION ALL
SELECT 09,N'Карачаево-Черкесская Республика', '61b95807-388a-4cb1-9bee-889f7cf811c8' UNION ALL
SELECT 89,N'Ямало-Ненецкий автономный округ (Тюменская область)', '826fa834-3ee8-404f-bdbc-13a5221cfb6e' UNION ALL
SELECT 59,N'Пермский край', '4f8b1a21-e4bb-422f-9087-d3cbf4bebc14' UNION ALL
SELECT 76,N'Ярославская область', 'a84b2ef4-db03-474b-b552-6229e801ae9b' UNION ALL
SELECT 24,N'Красноярский край', 'db9c4f8b-b706-40e2-b2b4-d31b98dcd3d1' UNION ALL
SELECT 45,N'Курганская область', '4a3d970f-520e-46b9-b16c-50d4ca7535a8' UNION ALL
SELECT 67,N'Смоленская область', 'e8502180-6d08-431b-83ea-c7038f0df905' UNION ALL
SELECT 49,N'Магаданская область', '9c05e812-8679-4710-b8cb-5e8bd43cdf48' UNION ALL
SELECT 92,N'г. Севастополь', '6fdecb78-893a-4e3f-a5ba-aa062459463b' UNION ALL
SELECT 40,N'Калужская область', '18133adf-90c2-438e-88c4-62c41656de70' UNION ALL
SELECT 34,N'Волгоградская область', 'da051ec8-da2e-4a66-b542-473b8d221ab4' UNION ALL
SELECT 86,N'Ханты-Мансийский автономный округ - Югра (Тюменская область)', 'd66e5325-3a25-4d29-ba86-4ca351d9704b' UNION ALL
SELECT 21,N'Чувашская Республика - Чувашия', '878fc621-3708-46c7-a97f-5a13a4176b3e' UNION ALL
SELECT 47,N'Ленинградская область', '6d1ebb35-70c6-4129-bd55-da3969658f5d' UNION ALL
SELECT 17,N'Республика Тыва', '026bc56f-3731-48e9-8245-655331f596c0' UNION ALL
SELECT 66,N'Свердловская область', '92b30014-4d52-4e2e-892d-928142b924bf' UNION ALL
SELECT 23,N'Краснодарский край', 'd00e1013-16bd-4c09-b3d5-3cb09fc54bd8' UNION ALL
SELECT 64,N'Саратовская область', 'df594e0e-a935-4664-9d26-0bae13f904fe' UNION ALL
SELECT 27,N'Хабаровский край', '7d468b39-1afa-41ec-8c4f-97a8603cb3d4' UNION ALL
SELECT 25,N'Приморский край', '43909681-d6e1-432d-b61f-ddac393cb5da' UNION ALL
SELECT 29,N'Архангельская область', '294277aa-e25d-428c-95ad-46719c4ddb44' UNION ALL
SELECT 43,N'Кировская область', '0b940b96-103f-4248-850c-26b6c7296728' UNION ALL
SELECT 07,N'Кабардино-Балкарская Республика', '1781f74e-be4a-4697-9c6b-493057c94818' UNION ALL
SELECT 13,N'Республика Мордовия', '37a0c60a-9240-48b5-a87f-0d8c86cdb6e1' UNION ALL
SELECT 06,N'Республика Ингушетия', 'b2d8cd20-cabc-4deb-afad-f3c4b4d55821' UNION ALL
SELECT 54,N'Новосибирская область', '1ac46b49-3209-4814-b7bf-a509ea1aecd9' UNION ALL
SELECT 11,N'Республика Коми', 'c20180d9-ad9c-46d1-9eff-d60bc424592a' UNION ALL
SELECT 79,N'Еврейская автономная область', '1b507b09-48c9-434f-bf6f-65066211c73e' UNION ALL
SELECT 01,N'Республика Адыгея (Адыгея)', 'd8327a56-80de-4df2-815c-4f6ab1224c50' UNION ALL
SELECT 33,N'Владимирская область', 'b8837188-39ee-4ff9-bc91-fcc9ed451bb3' UNION ALL
SELECT 73,N'Ульяновская область', 'fee76045-fe22-43a4-ad58-ad99e903bd58' UNION ALL
SELECT 42,N'Кемеровская область', '393aeccb-89ef-4a7e-ae42-08d5cebc2e30' UNION ALL
SELECT 44,N'Костромская область', '15784a67-8cea-425b-834a-6afe0e3ed61c' UNION ALL
SELECT 03,N'Республика Бурятия', 'a84ebed3-153d-4ba9-8532-8bdf879e1f5a' UNION ALL
SELECT 65,N'Сахалинская область', 'aea6280f-4648-460f-b8be-c2bc18923191' UNION ALL
SELECT 50,N'Московская область', '29251dcf-00a1-4e34-98d4-5c47484a36d4' UNION ALL
SELECT 91,N'Республика Крым', 'bd8e6511-e4b9-4841-90de-6bbc231a789e' UNION ALL
SELECT 26,N'Ставропольский край', '327a060b-878c-4fb4-8dc4-d5595871a3d8'
END