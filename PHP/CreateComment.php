<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$comment =  $_POST["comment"];
$session =  $_POST["session"];
$surfaceID = $_POST["surfaceID"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

// Create the table
$tableCreation = "CREATE TABLE IF NOT EXISTS c" . $clientId . "_p" . $projectId . "_comments ( id_surface INT UNSIGNED NOT NULL, comment VARCHAR(200), session DATETIME, PRIMARY KEY (id_surface, session) ) CHARACTER SET 'utf8' ENGINE=INNODB;";
$result = $bdd->query($tableCreation);

if ($result->errorCode() == 00000) 
{
  echo "Comment table creation: OK\r\n";
} 
else 
{
  echo "Error while creating the comment table!\r\n";
}
$result->closeCursor();

//Insert comment
$insertCmd = "REPLACE INTO c" . $clientId . "_p" . $projectId . "_comments (id_surface, comment, session) VALUES ( '" . $surfaceID . "', '" . $comment . "', '" . $session . "');";

$result = $bdd->query($insertCmd);

if ($result->errorCode() == 00000) 
{
  echo "Comment insertion: OK\r\n";
} 
else 
{
  echo "Error while inserting comment!\r\n";
}
$result->closeCursor();
?>