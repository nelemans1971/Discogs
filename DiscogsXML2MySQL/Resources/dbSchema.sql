USE <%DATABASE%>;

DROP TABLE IF EXISTS SETTING;
DROP TABLE IF EXISTS ARTIST;
DROP TABLE IF EXISTS NAMEVARIATION;
DROP TABLE IF EXISTS ALIAS;
DROP TABLE IF EXISTS MEMBER;
DROP TABLE IF EXISTS `GROUP`;
DROP TABLE IF EXISTS URL;
DROP TABLE IF EXISTS IMAGE;
DROP TABLE IF EXISTS LABEL;
DROP TABLE IF EXISTS SUBLABEL;
DROP TABLE IF EXISTS `RELEASE`;
DROP TABLE IF EXISTS RELEASE_LABEL;
DROP TABLE IF EXISTS RELEASE_ARTIST;
DROP TABLE IF EXISTS `FORMAT`;
DROP TABLE IF EXISTS FORMAT_DESCRIPTION;
DROP TABLE IF EXISTS GENRE;
DROP TABLE IF EXISTS STYLE;
DROP TABLE IF EXISTS TRACK;
DROP TABLE IF EXISTS TRACK_ARTIST;
DROP TABLE IF EXISTS IDENTIFIER;
DROP TABLE IF EXISTS VIDEO;
DROP TABLE IF EXISTS COMPANY;
DROP TABLE IF EXISTS MASTER;
DROP TABLE IF EXISTS MASTER_ARTIST;


# ---------------------------------------------------------------------- #
# Add table "ARTIST"                                                     #
# ---------------------------------------------------------------------- #

CREATE TABLE `ARTIST` (
    `ARTIST_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `NAME` VARCHAR(255) NOT NULL,
    `REALNAME` VARCHAR(1024),
    `PROFILE` TEXT,
    `DATA_QUALITY` VARCHAR(255),
    CONSTRAINT `PK_ARTIST` PRIMARY KEY (`ARTIST_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "NAMEVARIATION"                                              #
# ---------------------------------------------------------------------- #

CREATE TABLE `NAMEVARIATION` (
    `NAMEVARIATION_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `ARTIST_ID` INTEGER NOT NULL,
    `NAME` VARCHAR(255) NOT NULL,
    CONSTRAINT `PK_NAMEVARIATION` PRIMARY KEY (`NAMEVARIATION_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "ALIAS"                                                      #
# ---------------------------------------------------------------------- #

CREATE TABLE `ALIAS` (
    `ALIAS_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `MAIN_ARTIST_ID` INTEGER NOT NULL,
    `ALIAS_ARTIST_ID` INTEGER NOT NULL,
    CONSTRAINT `PK_ALIAS` PRIMARY KEY (`ALIAS_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "MEMBER"                                                     #
# ---------------------------------------------------------------------- #

CREATE TABLE `MEMBER` (
    `MEMBER_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `MAIN_ARTIST_ID` INTEGER NOT NULL,
    `MEMBER_ARTIST_ID` INTEGER NOT NULL,
    CONSTRAINT `PK_MEMBER` PRIMARY KEY (`MEMBER_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "GROUP"                                                      #
# ---------------------------------------------------------------------- #

CREATE TABLE `GROUP` (
    `GROUP_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `MAIN_ARTIST_ID` INTEGER NOT NULL,
    `GROUP_ARTIST_ID` INTEGER NOT NULL,
    CONSTRAINT `PK_GROUP` PRIMARY KEY (`GROUP_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "LABEL"                                                      #
# ---------------------------------------------------------------------- #

CREATE TABLE `LABEL` (
    `LABEL_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `PARENT_LABEL_ID` INTEGER,
    `NAME` VARCHAR(255) NOT NULL,
    `CONTACTINFO` VARCHAR(512),
    `PROFILE` LONGTEXT,
    `DATA_QUALITY` VARCHAR(255),
    CONSTRAINT `PK_LABEL` PRIMARY KEY (`LABEL_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "SUBLABEL"                                                   #
# ---------------------------------------------------------------------- #

CREATE TABLE `SUBLABEL` (
    `SUBLABEL_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `MAIN_LABEL_ID` INTEGER NOT NULL,
    `CHILD_LABEL_ID` INTEGER NOT NULL,
    CONSTRAINT `PK_SUBLABEL` PRIMARY KEY (`SUBLABEL_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "SETTING"                                                    #
# ---------------------------------------------------------------------- #

CREATE TABLE `SETTING` (
    `SETTING_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `NAME` VARCHAR(40) NOT NULL,
    `VALUE` VARCHAR(255) NOT NULL,
    CONSTRAINT `PK_SETTING` PRIMARY KEY (`SETTING_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "URL"                                                        #
# ---------------------------------------------------------------------- #

CREATE TABLE `URL` (
    `URL_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `ARTIST_ID` INTEGER,
    `LABEL_ID` INTEGER,
    `URL` VARCHAR(1024) NOT NULL,
    CONSTRAINT `PK_URL` PRIMARY KEY (`URL_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "IMAGE"                                                      #
# ---------------------------------------------------------------------- #

CREATE TABLE `IMAGE` (
    `IMAGE_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `ARTIST_ID` INTEGER,
    `LABEL_ID` INTEGER,
    `RELEASE_ID` INTEGER,
    `MASTER_ID` INTEGER,
    `HEIGHT` INTEGER,
    `WIDTH` INTEGER,
    `TYPE` VARCHAR(16),
    `URI` VARCHAR(255),
    `URI150` VARCHAR(255),
    CONSTRAINT `PK_IMAGE` PRIMARY KEY (`IMAGE_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "RELEASE"                                                    #
# ---------------------------------------------------------------------- #

CREATE TABLE `RELEASE` (
    `RELEASE_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `MASTER_ID` INTEGER NOT NULL,
    `STATUS` VARCHAR(40) NOT NULL,
    `TITLE` VARCHAR(255) NOT NULL,
    `COUNTRY` VARCHAR(50) NOT NULL,
    `RELEASED` DATE NOT NULL,
    `NOTES` LONGTEXT,
    `DATA_QUALITY` VARCHAR(40) NOT NULL,
    `IS_MAIN_RELEASE` BOOL NOT NULL DEFAULT 0,
    CONSTRAINT `PK_RELEASE` PRIMARY KEY (`RELEASE_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "RELEASE_LABEL"                                              #
# ---------------------------------------------------------------------- #

CREATE TABLE `RELEASE_LABEL` (
    `RELEASE_LABEL_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `RELEASE_ID` INTEGER NOT NULL,
    `LABEL_ID` INTEGER NOT NULL,
    `CATNO` VARCHAR(40) NOT NULL,
    CONSTRAINT `PK_RELEASE_LABEL` PRIMARY KEY (`RELEASE_LABEL_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "RELEASE_ARTIST"                                             #
# ---------------------------------------------------------------------- #

CREATE TABLE `RELEASE_ARTIST` (
    `RELEASE_ARTIST_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `RELEASE_ID` INTEGER NOT NULL,
    `ARTIST_ID` INTEGER NOT NULL,
    `ANV` VARCHAR(100) NOT NULL DEFAULT '',
    `JOIN` VARCHAR(100) NOT NULL DEFAULT '',
    `ROLE` VARCHAR(100) NOT NULL DEFAULT '',
    `NAME` VARCHAR(255) NOT NULL,
    `EXTRA_ARTIST` BOOL NOT NULL DEFAULT 0,
    CONSTRAINT `PK_RELEASE_ARTIST` PRIMARY KEY (`RELEASE_ARTIST_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "FORMAT"                                                     #
# ---------------------------------------------------------------------- #

CREATE TABLE `FORMAT` (
    `FORMAT_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `RELEASE_ID` INTEGER NOT NULL,
    `FORMAT_NAME` VARCHAR(100) NOT NULL,
    `FORMAT_TEXT` VARCHAR(255),
    `QUANTITY` INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT `PK_FORMAT` PRIMARY KEY (`FORMAT_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "FORMAT_DESCRIPTION"                                         #
# ---------------------------------------------------------------------- #

CREATE TABLE `FORMAT_DESCRIPTION` (
    `FORMAT_DESCRIPTION_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `FORMAT_ID` INTEGER NOT NULL,
    `DESCRIPTION` VARCHAR(100) NOT NULL,
    `DESCRIPTION_ORDER` INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT `PK_FORMAT_DESCRIPTION` PRIMARY KEY (`FORMAT_DESCRIPTION_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "GENRE"                                                      #
# ---------------------------------------------------------------------- #

CREATE TABLE `GENRE` (
    `GENRE_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `RELEASE_ID` INTEGER,
    `MASTER_ID` INTEGER,
    `GENRE_NAME` VARCHAR(100) NOT NULL,
    CONSTRAINT `PK_GENRE` PRIMARY KEY (`GENRE_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "STYLE"                                                      #
# ---------------------------------------------------------------------- #

CREATE TABLE `STYLE` (
    `STYLE_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `RELEASE_ID` INTEGER,
    `MASTER_ID` INTEGER,
    `STYLE_NAME` VARCHAR(100) NOT NULL,
    CONSTRAINT `PK_STYLE` PRIMARY KEY (`STYLE_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "TRACK"                                                      #
# ---------------------------------------------------------------------- #
CREATE TABLE `TRACK` (
    `TRACK_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `RELEASE_ID` INTEGER NOT NULL,
    `MAIN_TRACK_ID` INTEGER,
    `HAS_SUBTRACKS` BOOL NOT NULL DEFAULT 0,
    `IS_SUBTRACK` BOOL NOT NULL DEFAULT 0,
    `TRACKNUMBER` INTEGER NOT NULL DEFAULT 1,
    `TITLE` VARCHAR(255) NOT NULL,
    `SUBTRACK_TITLE` VARCHAR(255) NOT NULL DEFAULT '',
    `POSITION` VARCHAR(20) NOT NULL DEFAULT '',
    `DURATION_IN_SEC` INTEGER NOT NULL DEFAULT -1,
    CONSTRAINT `PK_TRACK` PRIMARY KEY (`TRACK_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "TRACK_ARTIST"                                               #
# ---------------------------------------------------------------------- #

CREATE TABLE `TRACK_ARTIST` (
    `TRACK_ARTIST_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `TRACK_ID` INTEGER NOT NULL,
    `ARTIST_ID` INTEGER NOT NULL,
    `ANV` VARCHAR(100) NOT NULL DEFAULT '',
    `JOIN` VARCHAR(100) NOT NULL DEFAULT '',
    `ROLE` VARCHAR(100) NOT NULL DEFAULT '',
    `NAME` VARCHAR(255) NOT NULL,
    `EXTRA_ARTIST` BOOL NOT NULL DEFAULT 0,
    CONSTRAINT `PK_TRACK_ARTIST` PRIMARY KEY (`TRACK_ARTIST_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "IDENTIFIER"                                                 #
# ---------------------------------------------------------------------- #

CREATE TABLE `IDENTIFIER` (
    `IDENTIFIER_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `RELEASE_ID` INTEGER NOT NULL,
    `DESCRIPTION` VARCHAR(100) NOT NULL,
    `TYPE` VARCHAR(40) NOT NULL,
    `VALUE` VARCHAR(100) NOT NULL,
    CONSTRAINT `PK_IDENTIFIER` PRIMARY KEY (`IDENTIFIER_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "VIDEO"                                                      #
# ---------------------------------------------------------------------- #

CREATE TABLE `VIDEO` (
    `VIDEO_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `RELEASE_ID` INTEGER NOT NULL,
    `MASTER_ID` INTEGER,
    `EMBED` BOOL NOT NULL DEFAULT 0,
    `DURATION_IN_SEC` INTEGER NOT NULL DEFAULT 0,
    `SRC` VARCHAR(255) NOT NULL,
    `TITLE` VARCHAR(255) NOT NULL,
    `DESCRIPTION` VARCHAR(255) NOT NULL,
    CONSTRAINT `PK_VIDEO` PRIMARY KEY (`VIDEO_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "COMPANY"                                                    #
# ---------------------------------------------------------------------- #

CREATE TABLE `COMPANY` (
    `COMPANY_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `RELEASE_ID` INTEGER NOT NULL,
    `NAME` VARCHAR(255) NOT NULL,
    `CATNO` VARCHAR(100) NOT NULL,
    `ENTITY_TYPE` INTEGER NOT NULL,
    `ENTITY_TYPE_NAME` VARCHAR(40) NOT NULL,
    `RESOURCE_URL` VARCHAR(255) NOT NULL DEFAULT '',
    CONSTRAINT `PK_COMPANY` PRIMARY KEY (`COMPANY_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "MASTER"                                                     #
# ---------------------------------------------------------------------- #

CREATE TABLE `MASTER` (
    `MASTER_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `MAIN_RELEASE_ID` INTEGER NOT NULL,
    `TITLE` VARCHAR(255) NOT NULL,
    `RELEASED` DATE NOT NULL,
    `NOTES` LONGTEXT,
    `DATA_QUALITY` VARCHAR(40) NOT NULL,
    CONSTRAINT `PK_MASTER` PRIMARY KEY (`MASTER_ID`)
);

# ---------------------------------------------------------------------- #
# Add table "MASTER_ARTIST"                                              #
# ---------------------------------------------------------------------- #

CREATE TABLE `MASTER_ARTIST` (
    `MASTER_ARTIST_ID` INTEGER NOT NULL AUTO_INCREMENT,
    `MASTER_ID` INTEGER NOT NULL,
    `ARTIST_ID` INTEGER NOT NULL,
    `ANV` VARCHAR(100) NOT NULL,
    `JOIN` VARCHAR(100) NOT NULL,
    `ROLE` VARCHAR(100) NOT NULL,
    `NAME` VARCHAR(255) NOT NULL,
    CONSTRAINT `PK_MASTER_ARTIST` PRIMARY KEY (`MASTER_ARTIST_ID`)
);
