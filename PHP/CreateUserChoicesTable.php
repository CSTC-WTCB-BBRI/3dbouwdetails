<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$tableCreation = "CREATE TABLE IF NOT EXISTS c" . $clientId . "_p" . $projectId . "_choices ( id_surface INT UNSIGNED NOT NULL, id_tile SMALLINT UNSIGNED NOT NULL, surface_price DOUBLE UNSIGNED, session DATETIME, PRIMARY KEY (id_surface, session) ) CHARACTER SET 'utf8' ENGINE=INNODB;";
$result = $bdd->query($tableCreation);

if ($result->errorCode() == 00000) 
{
  echo "User's choices table creation: OK\r\n";
} 
else 
{
  echo "Error while creating the user's choices table!\r\n";
}

//Close the query access
$result->closeCursor();
?>