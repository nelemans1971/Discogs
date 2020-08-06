USE <%DATABASE%>;


CREATE INDEX `IDX_IDENTIFIER_1` ON `IDENTIFIER` (`IDENTIFIER_ID`);
CREATE INDEX `IDX_IDENTIFIER_2` ON `IDENTIFIER` (`VALUE`);
CREATE INDEX `IDX_RELEASE_ARTIST_1` ON `RELEASE_ARTIST` (`RELEASE_ID`,`ARTIST_ID`);
CREATE INDEX `IDX_ALIAS_1` ON `ALIAS` (`MAIN_ARTIST_ID`,`ALIAS_ARTIST_ID`);
CREATE INDEX `IDX_MEMBER_1` ON `MEMBER` (`MAIN_ARTIST_ID`,`MEMBER_ARTIST_ID`);
CREATE INDEX `IDX_GROUP_1` ON `GROUP` (`MAIN_ARTIST_ID`,`GROUP_ARTIST_ID`);
CREATE INDEX `IDX_SUBLABEL_1` ON `SUBLABEL` (`MAIN_LABEL_ID`,`CHILD_LABEL_ID`);
CREATE UNIQUE INDEX `IDX_SETTING_1` ON `SETTING` (`NAME`);
CREATE INDEX `IDX_RELEASE_LABEL_1` ON `RELEASE_LABEL` (`RELEASE_ID`,`LABEL_ID`);
CREATE INDEX `IDX_RELEASE_LABEL_2` ON `RELEASE_LABEL` (`CATNO`);
CREATE INDEX `IDX_RELEASE_1` ON `RELEASE` (`MASTER_ID`);
CREATE INDEX `IDX_FORMAT_1` ON `FORMAT` (`RELEASE_ID`);
CREATE INDEX `IDX_TRACK_1` ON `TRACK` (`RELEASE_ID`);
CREATE INDEX `IDX_TRACK_2` ON `TRACK` (`MAIN_TRACK_ID`);
CREATE INDEX `IDX_FORMAT_DESCRIPTION_1` ON `FORMAT_DESCRIPTION` (`FORMAT_DESCRIPTION_ID`);
CREATE INDEX `IDX_FORMAT_DESCRIPTION_2` ON `FORMAT_DESCRIPTION` (`FORMAT_ID`);
CREATE INDEX `IDX_TRACK_ARTIST_1` ON `TRACK_ARTIST` (`TRACK_ID`);
CREATE INDEX `IDX_COMPANY_1` ON `COMPANY` (`COMPANY_ID`);
CREATE INDEX `IDX_COMPANY_2` ON `COMPANY` (`CATNO`);
CREATE INDEX `IDX_VIDEO_1` ON `VIDEO` (`VIDEO_ID`);
