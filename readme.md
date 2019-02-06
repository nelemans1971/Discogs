## DiscogsXML2MySQL

This software was developed at [www.muziekweb.nl](https://www.muziekweb.nl) by
Yvo Nelemans. We are a public library in the Netherlands for lending cd's, lp's
and dvd's. We have a collection of over 500.000 physical objects we lend out to
our lenders in the Netherlands.


### What does it do

This program is meant to convert the monthly xml export from
[discogs](https://data.discogs.com/) and import them into a mysql database. The
last part of importing them into a mysql database is optional, you can chose to
only convert the xml files to tab seperated files and leave it at that.

**The program does the following steps:**

1. Downloads the 4 .gz files from discogs and unzips them to 4 xml files
2. Convert the xml files (after some cleanup) to tab files
3. When a xml file is completly converted to tab bulk import it into mysql (optional)
4. After all data is imported (some) indexes are created

The tab files are stored in the location where the DiscogsXML2MySQL.exe files is
located.


### Used packages

[ini-parser](https://github.com/rickyah/ini-parser)<br/>
[MySql.Data.dll](https://dev.mysql.com/doc/connector-net/en/)<br/>
[Logging framework](http://www.theobjectguy.com/DotNetLog/)<br/>

### License

The code is copyrighted 2019 by Stichting Centrale Discotheek Rotterdam, my
employer, and licensed under the [MIT license](https://opensource.org/licenses/MIT).
Specific parts of the code are written by others and are copyrighted by their
respective holders.


### Configurating .ini file before use
Under the section [Program] the are 4 options.<br/>
smtp and email can be left empty if you don't want to get an e-mail that
something went wrong.<br/>
DiscogsDataURL is allready filled in and should not be changed.<br/>
DataPath is the directory where the downloaded .gz/.xml files are stored (NOT
the tab files!)

Under the section [DBConnnection] the database connection setting details need
to be entered. if you only use the program with the option /onlytab you can
ignore these settings.

The function RenameINIFile in Program.cs references 2 inifiles (DiscogsXML2MySQL.DEBUG.ini and 
DiscogsXML2MySQL.RELEASE.ini) which are not included. They are used for settings which I cannot 
sync to GitHub. 

Example of DiscogsXML2MySQL.ini:
[Program]<br/>
smtp=<br/>
email=<br/>
DiscogsDataURL=https://data.discogs.com<br/>
DataPath=.\Data<br/>
<br/>
[DBConnnection]<br/>
mysqlServer=localhost<br/>
mysqlServerPort=3306<br/>
mysqlDB=discogs<br/>
mysqlUser=root<br/>
mysqlPassword=123456<br/>


### Commandline options

**/forcedownload**<br/>
Always (re)download the last discogs gz files and unzip them.<br/>
**/forceupdate**<br/>
The date of the discogs export files is stored in the table 'SETTINGS', when this
is the same as the file date, the data is not imported. This flag forces the program
to ignore the date.<br/>
**/onlytab**<br/>
This flag will only create the TAB files (and not throw them away when importing
them in the database). Below you can find the reference for the layout of the
fields per file.
**/useexistingfiles**<br/>
This flag stops the program from downloading the latest files from discogs and
uses the files in the data directory instead (see ini file where this is
defined).


### Log files

Most of the output from DiscogsXML2MySQL is written to a logfile with the name
DiscogsXML2MySQL-yyyy-mm.log this is done because most of the time this program
is run unattended and when something goes wrong it makes the job of debug the
problem a lot easier.<br/>
Another log file DiscogsXML2MySQL.log is also created and mostly contains the
start timestamps. Exceptions created by the program are also logged in this
file.


### mysql

I've only tested the import into mysql with some settings which could break it
on other systems if they are not set (not tested so I don't know).<br>
**lower_case_table_names=1**<br/>
We have this set on, because in the past we had a mix of windows and linux
servers and name casing could lead to trouble on one server and not on the
other. This forces them to always convert to lower case.

The xml files contain all kind of characters. utf8 is needed. On mysql this
means the database, tables and fields must use utf8mb4 as character set (not
utf8!). The program checks if the database is created with this option and stops
if not!


### Why did I create this program

The company I work for wants/wanted to match our music data with discogs. To do
this would mean a lot of web service request, which are limited to 1 per second.
This would be to slow for us. We then looked at using the discogs monthly
export. We allready use mysql for our own website and wanted to use this
database for matching. After some searching I couldn't find a good conversion
program for the discogs data so that's why we created our own.<br/>
The program is written in C# and we asume you run it on a windows pc (haven't
tested it with mono). The mysql part can of course be on a linux machine.


### Some statistics

Download from discogs and unzipping the files takes about 1 hour and 21
minutes.<br/>
Converting the xml files and importing them to mysql takes about 3 hours on a
vmware server with SSD and 4 hours was on a bare bone server with five disk in
raid5.<br/>
Converting the xml files only to tab files take about 2 hour and 10 minutes on a
desktop machine with SSD.<br/>


### TAB files and the fields

Some tab files contain ID's and some not, the decision when or not to include them
is based on the datamodel I used.

**discogs_yyyymmdd_artists.xml**

* ARTIST.TAB
  * Fields: ARTIST_ID, NAME, REALNAME, PROFILE, DATA_QUALITY
* NAMEVARIATION.TAB
  * Fields: ARTIST_ID, NAME
* ALIAS.TAB
  * Fields: MAIN_ARTIST_ID, ALIAS_ARTIST_ID
* MEMBER.TAB
  * Fields: MAIN_ARTIST_ID, MEMBER_ARTIST_ID
* GROUP.TAB
  * Fields: MAIN_ARTIST_ID, GROUP_ARTIST_ID
* IMAGE.TAB
  * Fields: ARTIST_ID, HEIGHT, WIDTH, TYPE, URI, URI150
* URL.TAB
  * Fields: ARTIST_ID, URL

**discogs_yyyymmdd_labels.xml**

* LABEL.TAB
  * Fields: LABEL_ID, NAME, CONTACTINFO, PROFILE, DATA_QUALITY, PARENT_LABEL_ID
* LABEL-IMAGE.TAB
  * Fields: LABEL_ID, HEIGHT, WIDTH, TYPE, URI, URI150
* LABEL-URL.TAB
  * Fields: LABEL_ID, URL
* SUBLABEL.TAB
  * Fields: MAIN_LABEL_ID, CHILD_LABEL_ID

**discogs_yyyymmdd_releases.xml**

* RELEASE.TAB
  * Fields: RELEASE_ID, MASTER_ID, STATUS, TITLE, COUNTRY, RELEASED, NOTES, DATA_QUALITY, IS_MAIN_RELEASE
* RELEASE-IMAGE.TAB
  * Fields: RELEASE_ID, HEIGHT, WIDTH, TYPE, URI, URI150
* RELEASE-ARTIST.TAB
  * Fields: RELEASE_ID, ARTIST_ID, ANV, `JOIN`, ROLE, `NAME`, EXTRA_ARTIST
* RELEASE-GENRE.TAB
  * Fields: RELEASE_ID, GENRE_NAME
* RELEASE-STYLE.TAB
  * Fields: RELEASE_ID, STYLE_NAME
* FORMAT.TAB
  * Fields: FORMAT_ID, RELEASE_ID, FORMAT_NAME, FORMAT_TEXT, QUANTITY
* FORMAT-DESCRIPTION.TAB
  * Fields: FORMAT_ID, DESCRIPTION, DESCRIPTION_ORDER
* RELEASE-LABEL.TAB
  * Fields: RELEASE_ID, LABEL_ID, CATNO
* TRACK.TAB
  * Fields: TRACK_ID, RELEASE_ID, MAIN_TRACK_ID, HAS_SUBTRACKS, IS_SUBTRACK, TRACKNUMBER, TITLE, SUBTRACK_TITLE, POSITION, DURATION_IN_SEC
* TRACK-ARTIST.TAB
  * Fields: TRACK_ID, ARTIST_ID, ANV, JOIN, ROLE, `NAME`, EXTRA_ARTIST
* IDENTIFIER.TAB
  * Fields: RELEASE_ID, DESCRIPTION, TYPE, VALUE
* RELEASE-VIDEO.TAB
  * Fields: RELEASE_ID, EMBED, DURATION_IN_SEC, SRC, TITLE, `DESCRIPTION`
* COMPANY.TAB
  * Fields: COMPANY_ID, RELEASE_ID, NAME, CATNO, ENTITY_TYPE, ENTITY_TYPE_NAME, RESOURCE_URL

**discogs_yyyymmdd_masters.xml**

* MASTER.TAB
  * Fields: MASTER_ID, MAIN_RELEASE_ID, TITLE, RELEASED, NOTES, DATA_QUALITY
* MASTER-IMAGE.TAB
  * Fields: MASTER_ID, HEIGHT, WIDTH, TYPE, URI, URI150
* MASTER-ARTIST.TAB
  * Fields: MASTER_ID, ARTIST_ID, ANV, JOIN, NAME, ROLE
* MASTER-GENRE.TAB
  * Fields: MASTER_ID, GENRE_NAME
* MASTER-STYLE.TAB
  * Fields: MASTER_ID, STYLE_NAME
* MASTER-VIDEO.TAB
  * Fields: MASTER_ID, EMBED, DURATION_IN_SEC, SRC, TITLE, DESCRIPTION


### Datamodel

The datamodel is made in [DeZign for Databases](https://www.datanamic.com/). I've
also included the .dez file and a .emf image file for reference.

![Screenshot Datamodel](https://github.com/nelemans1971/Discogs/blob/master/DISCOGS.png?raw=true)


### Questions?
You can contact me (Yvo Nelemans) at y.nelemans@muziekweb.nl
