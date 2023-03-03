<?php

$session = $_POST["session"];
$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$surfaces_ids = $_POST["ids"];
$comments = $_POST["comments"];

$nb_comments = count($surfaces_ids);

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

// Insert comments
for($i = 0; $i < $nb_comments; $i++) {
	$insertCmd = "REPLACE INTO c" . $clientId . "_p" . $projectId . "_comments (id_surface, comment, session) VALUES ( '" . $surfaces_ids[$i] . "', '" . $comments[$i] . "', '" . $session . "');";
	echo $insertCmd;
	$result = $bdd->query($insertCmd);
	if ($result->errorCode() == 00000) {
  		echo "Screenshot insertion: OK\r\n";
	} 
	else {
  		echo "Error while inserting screenshot!\r\n";
  		break;
	}
}

$result = $bdd->query($insertCmd);

if ($result->errorCode() == 00000) 
{
  echo "User's comments table update: OK\r\n";
} 
else 
{
  echo "Error while updating the user's comments table!\r\n";
}

//Close the query access
$result->closeCursor();
?>